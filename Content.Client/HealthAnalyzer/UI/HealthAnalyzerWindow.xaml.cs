using Content.Client._White.Medical.BodyScanner;
using System.Linq;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.HealthAnalyzer.UI
{
    // WD start
    [GenerateTypedNameReferences]
    public sealed partial class HealthAnalyzerWindow : FancyWindow
    {
        public HealthAnalyzerWindow()
        {
            RobustXamlLoader.Load(this);
        }

        public void Populate(HealthAnalyzerScannedUserMessage msg)
        {
            var entities = IoCManager.Resolve<IEntityManager>();

            if (msg.TargetEntity == null || !entities.TryGetComponent(entities.GetEntity(msg.TargetEntity),
                    out DamageableComponent? damageable))
            {
                return;
            }

            EntityNameLabel.Text = Identity.Name(entities.GetEntity(msg.TargetEntity.Value), entities);
            TemperatureLabel.Text = float.IsNaN(msg.Temperature)
                ? Loc.GetString("health-analyzer-window-no-data")
                : $"{msg.Temperature - 273f:F1} \u00B0C";

            BloodLevelLabel.Text = float.IsNaN(msg.BloodLevel)
                ? Loc.GetString("health-analyzer-window-no-data")
                : $"{msg.BloodLevel * 100:F1} %";

            TotalDamageLabel.Text = damageable.TotalDamage.ToString();

            entities.TryGetComponent<MobStateComponent>(entities.GetEntity(msg.TargetEntity),
                out var mobStateComponent);

            AliveStatusLabel.Text = mobStateComponent?.CurrentState switch
            {
                Shared.Mobs.MobState.Alive => Loc.GetString(
                    "health-analyzer-window-entity-current-alive-status-alive-text"),
                Shared.Mobs.MobState.Critical => Loc.GetString(
                    "health-analyzer-window-entity-current-alive-status-critical-text"),
                Shared.Mobs.MobState.Dead => Loc.GetString(
                    "health-analyzer-window-entity-current-alive-status-dead-text"),
                _ => Loc.GetString("health-analyzer-window-no-data"),
            };

            IReadOnlyDictionary<string, FixedPoint2> damagePerGroup = damageable.DamagePerGroup;
            IReadOnlyDictionary<string, FixedPoint2> damagePerType = damageable.Damage.DamageDict;

            DamageGroupsContainer.RemoveAllChildren();

            // Show the total damage and type breakdown for each damage group.
            foreach (var (damageGroupId, damageAmount) in damagePerGroup)
            {
                if (damageAmount == 0)
                {
                    continue;
                }

                var damageGroupTitle = Loc.GetString("health-analyzer-window-damage-group-" + damageGroupId,
                    ("amount", damageAmount));

                DamageGroupsContainer.AddChild(new GroupDamageCardComponent(damageGroupTitle, damageGroupId,
                    damagePerType));
            }
        }
    }
    // WD end
}
