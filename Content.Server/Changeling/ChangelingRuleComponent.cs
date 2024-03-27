using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Changeling;

[RegisterComponent, Access(typeof(ChangelingRuleSystem))]
public sealed partial class ChangelingRuleComponent : Component
{
    public readonly List<EntityUid> ChangelingMinds = new() { };

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string ChangelingPrototypeId = "Changeling";

    public int TotalChangelings => ChangelingMinds.Count;

    public enum SelectionState
    {
        WaitingForSpawn = 0,
        ReadyToStart = 1,
        Started = 2,
    }

    public SelectionState SelectionStatus = SelectionState.WaitingForSpawn;
    public TimeSpan AnnounceAt = TimeSpan.Zero;

    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField("greetSoundNotification")]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/changeling_start.ogg");
}