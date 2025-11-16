using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace DynamicNobleDeparture
{
    public class NobleDepartureBehavior : CampaignBehaviorBase
    {
        private DynamicNobleDepartureSettings S
            => DynamicNobleDepartureSettings.Instance;

        public override void RegisterEvents()
            => CampaignEvents.DailyTickEvent
               .AddNonSerializedListener(this, OnDailyTick);

        public override void SyncData(IDataStore dataStore) { }

        private void OnDailyTick()
        {
            var s = S;
            foreach (var clan in Clan.All.Where(c => !c.IsEliminated))
            {
                foreach (var hero in clan.Heroes)
                {
                    if (hero == Hero.MainHero || hero.IsClanLeader)
                        continue;

                    if (Clan.All.Count(c => !c.IsEliminated) >= s.MaxActiveClans)
                        return;

                    int relation = hero.GetRelation(clan.Leader);
                    float chance = s.LeavingChance
                                 + s.RelationWeight * (100 - relation) / 100f;
                    if (MBRandom.RandomFloat >= chance)
                        continue;

                    ProcessDeparture(hero, clan, s);
                }
            }
        }

        private void ProcessDeparture(
            Hero hero, Clan oldClan, DynamicNobleDepartureSettings s)
        {
            float total = s.JoinWeight + s.NewClanWeight + s.WandererWeight;
            float roll = MBRandom.RandomFloat * total;

            Clan newClan = null;
            bool foundedNew = false;

            if (roll < s.JoinWeight)
            {
                newClan = Clan.All
                    .Where(c => c != oldClan && !c.IsEliminated)
                    .OrderByDescending(c => hero.GetRelation(c.Leader))
                    .FirstOrDefault();
                if (newClan != null) hero.Clan = newClan;
            }
            else if (roll < s.JoinWeight + s.NewClanWeight)
            {
                var home = hero.HomeSettlement
                             ?? Clan.PlayerClan.HomeSettlement;
                var nameTO = new TextObject("House of {HERO}")
                    .SetTextVariable("HERO", hero.Name.ToString());
                newClan = Clan.CreateCompanionToLordClan(
                    hero, home, nameTO, 0);
                newClan.SetLeader(hero);
                foundedNew = true;
            }
            else
            {
                hero.ChangeState(Hero.CharacterStates.Fugitive);
            }

            if (s.IncludeSpouse && hero.Spouse?.Clan != null)
                hero.Spouse.Clan = hero.Clan;
            if (s.IncludeChildren)
                foreach (var child in hero.Children)
                    if (child.Clan != null)
                        child.Clan = hero.Clan;

            bool isPlayerClan = oldClan == Clan.PlayerClan;
            if ((isPlayerClan && s.ShowPlayerMessages) ||
                (!isPlayerClan && s.ShowOtherMessages))
            {
                ShowMessage(hero, oldClan, newClan, foundedNew);
            }
        }

        private void ShowMessage(
            Hero hero, Clan oldClan, Clan newClan, bool foundedNew)
        {
            string id = newClan != null ? "DND001"
                       : foundedNew ? "DND002"
                                      : "DND003";

            var text = new TextObject($"{{={id}}}")
                .SetTextVariable("HERO", hero.Name.ToString())
                .SetTextVariable("OLD_CLAN", oldClan.Name.ToString());
            if (newClan != null)
                text.SetTextVariable("NEW_CLAN", newClan.Name.ToString());

            InformationManager.DisplayMessage(
                new InformationMessage(text.ToString()));
        }
    }
}
