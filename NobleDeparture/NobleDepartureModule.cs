using HarmonyLib;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NobleDeparture
{
    public class NobleDepartureModule : MBSubModuleBase
    {
        private Harmony _harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            InformationManager.DisplayMessage(
                new InformationMessage("Noble Departure Mod loaded successfully."));

            // Create a Harmony instance with a unique ID
            _harmony = new Harmony("NobleDeparture");
            // Tell Harmony to scan your assembly for [HarmonyPatch] classes
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();

        }

        protected override void OnGameStart(Game game, IGameStarter starter)
        {
            base.OnGameStart(game, starter);
            if (game.GameType is Campaign)
            {
                ((CampaignGameStarter)starter).AddBehavior(new NobleDepartureBehavior());
            }
        }
    }
}