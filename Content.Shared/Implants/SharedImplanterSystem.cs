using System.Diagnostics.CodeAnalysis;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Implants;

//WD EDIT START
public sealed class SubdermalImplantInserted
{
    /// <summary>
    ///     Entity who implants
    /// </summary>
    public EntityUid User;

    /// <summary>
    ///     Entity being implanted
    /// </summary>
    public EntityUid Target;

    public SubdermalImplantComponent Component;

    public SubdermalImplantInserted(EntityUid user, EntityUid target, SubdermalImplantComponent component)
    {
        User = user;
        Target = target;
        Component = component;
    }
}

public sealed class SubdermalImplantRemoved
{
    /// <summary>
    ///     Entity who removes implant
    /// </summary>
    public EntityUid User;

    /// <summary>
    ///     Entity which implant is removing
    /// </summary>
    public EntityUid Target;

    public SubdermalImplantComponent Component;

    public SubdermalImplantRemoved(EntityUid user, EntityUid target, SubdermalImplantComponent component)
    {
        User = user;
        Target = target;
        Component = component;
    }
}

//WD EDIT END

public abstract class SharedImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, ComponentInit>(OnImplanterInit);
        SubscribeLocalEvent<ImplanterComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<ImplanterComponent, ExaminedEvent>(OnExamine);
    }

    private void OnImplanterInit(EntityUid uid, ImplanterComponent component, ComponentInit args)
    {
        if (component.Implant != null)
            component.ImplanterSlot.StartingItem = component.Implant;

        _itemSlots.AddItemSlot(uid, ImplanterComponent.ImplanterSlotId, component.ImplanterSlot);
    }

    private void OnEntInserted(EntityUid uid, ImplanterComponent component, EntInsertedIntoContainerMessage args)
    {
        var implantData = EntityManager.GetComponent<MetaDataComponent>(args.Entity);
        component.ImplantData = (implantData.EntityName, implantData.EntityDescription);
    }

    private void OnExamine(EntityUid uid, ImplanterComponent component, ExaminedEvent args)
    {
        if (!component.ImplanterSlot.HasItem || !args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("implanter-contained-implant-text", ("desc", component.ImplantData.Item2)));
    }

    //Instantly implant something and add all necessary components and containers.
    //Set to draw mode if not implant only
    public void Implant(EntityUid user, EntityUid target, EntityUid implanter, ImplanterComponent component)
    {
        if (!CanImplant(user, target, implanter, component, out var implant, out var implantComp))
            return;

        //If the target doesn't have the implanted component, add it.
        var implantedComp = EnsureComp<ImplantedComponent>(target);
        var implantContainer = implantedComp.ImplantContainer;

        if (component.ImplanterSlot.ContainerSlot != null)
            _container.Remove(implant.Value, component.ImplanterSlot.ContainerSlot);

        implantComp.ImplantedEntity = target;
        implantContainer.OccludesLight = false;
        _container.Insert(implant.Value, implantContainer);
        RaiseLocalEvent(implant.Value, new SubdermalImplantInserted(user, target, implantComp)); //WD EDIT

        if (component.CurrentMode == ImplanterToggleMode.Inject && !component.ImplantOnly)
            DrawMode(implanter, component);
        else
            ImplantMode(implanter, component);

        var ev = new TransferDnaEvent { Donor = target, Recipient = implanter };
        RaiseLocalEvent(target, ref ev);

        Dirty(implanter, component);
    }

    public bool CanImplant(
        EntityUid user,
        EntityUid target,
        EntityUid implanter,
        ImplanterComponent component,
        [NotNullWhen(true)] out EntityUid? implant,
        [NotNullWhen(true)] out SubdermalImplantComponent? implantComp)
    {
        implant = component.ImplanterSlot.ContainerSlot?.ContainedEntities.FirstOrNull();
        if (!TryComp(implant, out implantComp))
            return false;

        if (!CheckTarget(target, component.Whitelist, component.Blacklist) ||
            !CheckTarget(target, implantComp.Whitelist, implantComp.Blacklist))
        {
            return false;
        }

        var ev = new AddImplantAttemptEvent(user, target, implant.Value, implanter);
        RaiseLocalEvent(target, ev);
        return !ev.Cancelled;
    }

    protected bool CheckTarget(EntityUid target, EntityWhitelist? whitelist, EntityWhitelist? blacklist)
    {
        return whitelist?.IsValid(target, EntityManager) != false &&
            blacklist?.IsValid(target, EntityManager) != true;
    }

    //Draw the implant out of the target
    //TODO: Rework when surgery is in so implant cases can be a thing
    public void Draw(EntityUid implanter, EntityUid user, EntityUid target, ImplanterComponent component)
    {
        var implanterContainer = component.ImplanterSlot.ContainerSlot;

        if (implanterContainer is null)
            return;

        var permanentFound = false;

        if (!_container.TryGetContainer(target, ImplanterComponent.ImplantSlotId, out var implantContainer))
            return;

        var implantCompQuery = GetEntityQuery<SubdermalImplantComponent>();

        foreach (var implant in implantContainer.ContainedEntities)
        {
            if (!implantCompQuery.TryGetComponent(implant, out var implantComp))
                continue;

            //Don't remove a permanent implant and look for the next that can be drawn
            if (!_container.CanRemove(implant, implantContainer))
            {
                var implantName = Identity.Entity(implant, EntityManager);
                var targetName = Identity.Entity(target, EntityManager);
                var failedPermanentMessage = Loc.GetString("implanter-draw-failed-permanent",
                    ("implant", implantName), ("target", targetName));

                _popup.PopupEntity(failedPermanentMessage, target, user);
                permanentFound = implantComp.Permanent;
                continue;
            }

            RaiseLocalEvent(implant, new SubdermalImplantRemoved(user, target, implantComp)); // WD EDIT
            _container.Remove(implant, implantContainer);

            implantComp.ImplantedEntity = null;
            _container.Insert(implant, implanterContainer);
            permanentFound = implantComp.Permanent;

            var ev = new TransferDnaEvent { Donor = target, Recipient = implanter };
            RaiseLocalEvent(target, ref ev);

            //Break so only one implant is drawn
            break;
        }

        if (component.CurrentMode == ImplanterToggleMode.Draw && !component.ImplantOnly && !permanentFound)
            ImplantMode(implanter, component);

        Dirty(implanter, component);
    }

    private void ImplantMode(EntityUid uid, ImplanterComponent component)
    {
        component.CurrentMode = ImplanterToggleMode.Inject;
        ChangeOnImplantVisualizer(uid, component);
    }

    private void DrawMode(EntityUid uid, ImplanterComponent component)
    {
        component.CurrentMode = ImplanterToggleMode.Draw;
        ChangeOnImplantVisualizer(uid, component);
    }

    private void ChangeOnImplantVisualizer(EntityUid uid, ImplanterComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var implantFound = component.ImplanterSlot.HasItem;

        switch (component.CurrentMode)
        {
            case ImplanterToggleMode.Inject when !component.ImplantOnly:
                _appearance.SetData(uid, ImplanterVisuals.Full, implantFound, appearance);
                break;
            case ImplanterToggleMode.Inject when component.ImplantOnly:
                _appearance.SetData(uid, ImplanterVisuals.Full, implantFound, appearance);
                _appearance.SetData(uid, ImplanterImplantOnlyVisuals.ImplantOnly, component.ImplantOnly,
                    appearance);

                break;
            case ImplanterToggleMode.Draw:
            default:
                _appearance.SetData(uid, ImplanterVisuals.Full, implantFound, appearance);
                break;
        }
    }
}

[Serializable, NetSerializable]
public sealed partial class ImplantEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class DrawEvent : SimpleDoAfterEvent
{
}

public sealed class AddImplantAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Target;
    public readonly EntityUid Implant;
    public readonly EntityUid Implanter;

    public AddImplantAttemptEvent(EntityUid user, EntityUid target, EntityUid implant, EntityUid implanter)
    {
        User = user;
        Target = target;
        Implant = implant;
        Implanter = implanter;
    }
}
