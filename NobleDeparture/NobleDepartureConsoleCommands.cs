using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace NobleDeparture
{
    public class NobleDepartureConsoleCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("debug_force_departure_hero", "nobledeparture")]
        private static string DebugForceDeparture(List<string> args)
        {


            if (args.Count <= 0) return "Hero not specified. Use nobledeparture.debug_force_departure_hero HeroNameHere";
            var hero = Hero.AllAliveHeroes
                .FirstOrDefault(h => h.Name.ToString() == args[0]);

            if (hero == null) return "Hero not found (Or dead)";
            if (hero.Clan == null) return "Hero is clanless";
            if (hero == hero.Clan.Leader) return "Hero is clan leader";
            if (hero.IsChild) return "Hero is child";

            var behavior = Campaign.Current.GetCampaignBehavior<NobleDepartureBehavior>();
            behavior.ForceDepartureCheck(hero);
            return $"Starting departure process for {hero.Name} from {hero.Clan.Name}";
        }
    }
}
