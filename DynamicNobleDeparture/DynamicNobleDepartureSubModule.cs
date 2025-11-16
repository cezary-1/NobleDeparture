using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using MCM.Abstractions.Base;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.Settings.Base;

namespace DynamicNobleDeparture
{
    public class DynamicNobleDepartureSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            // Build and register the MCM settings screen at runtime
            var screenBuilder = SettingsScreenHandler.CreateSettingsScreen("Dynamic Noble Departure");

            // General group
            screenBuilder.AddGroup("General", group =>
            {
                group.AddFloatingInteger(
                    id: "LeavingChance",
                    name: "Base Daily Leave Chance",
                    minValue: 0f, maxValue: 1f,
                    settingsRef: new SettingsRef<float>(
                        () => DynamicNobleDepartureSettings.LeavingChance,
                        v => DynamicNobleDepartureSettings.LeavingChance = v
                    )
                );
                group.AddFloatingInteger(
                    id: "RelationWeight",
                    name: "Relation Impact on Chance",
                    minValue: 0f, maxValue: 5f,
                    settingsRef: new SettingsRef<float>(
                        () => DynamicNobleDepartureSettings.RelationWeight,
                        v => DynamicNobleDepartureSettings.RelationWeight = v
                    )
                );
            });

            // Destinations group
            screenBuilder.AddGroup("Destinations", group =>
            {
                group.AddFloatingInteger(
                    "JoinWeight", "Join Existing Clan Weight", 0f, 5f,
                    new SettingsRef<float>(
                        () => DynamicNobleDepartureSettings.JoinWeight,
                        v => DynamicNobleDepartureSettings.JoinWeight = v
                    )
                );
                group.AddFloatingInteger(
                    "NewClanWeight", "Found New Clan Weight", 0f, 5f,
                    new SettingsRef<float>(
                        () => DynamicNobleDepartureSettings.NewClanWeight,
                        v => DynamicNobleDepartureSettings.NewClanWeight = v
                    )
                );
                group.AddFloatingInteger(
                    "WandererWeight", "Become Wanderer Weight", 0f, 5f,
                    new SettingsRef<float>(
                        () => DynamicNobleDepartureSettings.WandererWeight,
                        v => DynamicNobleDepartureSettings.WandererWeight = v
                    )
                );
            });

            // Limits group
            screenBuilder.AddGroup("Limits", group =>
            {
                group.AddInteger(
                    "MaxActiveClans", "Maximum Active Clans", 1, 200,
                    new SettingsRef<int>(
                        () => DynamicNobleDepartureSettings.MaxActiveClans,
                        v => DynamicNobleDepartureSettings.MaxActiveClans = v
                    )
                );
            });

            // Family group
            screenBuilder.AddGroup("Family", group =>
            {
                group.AddToggle(
                    "IncludeSpouse", "Include Spouse",
                    new SettingsRef<bool>(
                        () => DynamicNobleDepartureSettings.IncludeSpouse,
                        v => DynamicNobleDepartureSettings.IncludeSpouse = v
                    )
                );
                group.AddToggle(
                    "IncludeChildren", "Include Children",
                    new SettingsRef<bool>(
                        () => DynamicNobleDepartureSettings.IncludeChildren,
                        v => DynamicNobleDepartureSettings.IncludeChildren = v
                    )
                );
            });

            // Messages group
            screenBuilder.AddGroup("Messages", group =>
            {
                group.AddToggle(
                    "ShowPlayerMessages", "Show Player Clan Messages",
                    new SettingsRef<bool>(
                        () => DynamicNobleDepartureSettings.ShowPlayerMessages,
                        v => DynamicNobleDepartureSettings.ShowPlayerMessages = v
                    )
                );
                group.AddToggle(
                    "ShowOtherMessages", "Show Other Clan Messages",
                    new SettingsRef<bool>(
                        () => DynamicNobleDepartureSettings.ShowOtherMessages,
                        v => DynamicNobleDepartureSettings.ShowOtherMessages = v
                    )
                );
            });

            SettingsScreenHandler.RegisterSettingsScreen(screenBuilder.Build());
        }

        protected override void OnGameStart(Game game, IGameStarter starter)
        {
            base.OnGameStart(game, starter);
            if (game.GameType is Campaign)
            {
                ((CampaignGameStarter)starter)
                    .AddBehavior(new NobleDepartureBehavior());
            }
        }
    }
}
