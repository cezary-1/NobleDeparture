using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace NobleDeparture
{
    public class NobleDepartureBehavior : CampaignBehaviorBase
    {
        private bool _fatalErrorLogged;
        private double day;

        private Dictionary<string, double> traitMods;
        private Dictionary<string, double> chanceMap;
        private Dictionary<string, double> joinMap;
        private Dictionary<string, double> createMap;
        private List<string> clanSuffixes;
        private Dictionary<string, double> wanderMap;
        private Dictionary<string, string> kingdomMap;

        //trait mods, culture etc

        public NobleDepartureBehavior()
        {
            NobleDepartureSettings.Instance.DivorceWanderers = DivorceAll;
        }

        public override void RegisterEvents()
        {
            // Show config on load
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            // Run daily departure logic
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);

            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        //Dialogues

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            KickDial(starter);
        }

        private void KickDial(CampaignGameStarter game)
        {
            var s = NobleDepartureSettings.Instance;
            if (s == null) return;
            if (!s.KickDial) return;
            // 1) Root ask if you can speak with other hero
            game.AddPlayerLine(
                "nd_kick_hero_root",
                "hero_main_options",
                "nd_kick_hero_menu",
                "{=nd_kick_hero_root}I think you should leave our clan.",
                new ConversationSentence.OnConditionDelegate(() =>
                    Hero.OneToOneConversationHero != null &&
                    Hero.OneToOneConversationHero.Clan == Clan.PlayerClan &&
                    Hero.OneToOneConversationHero.IsLord
                ),
                null, 100, null, null
            );
            game.AddDialogLine(
                "nd_kick_hero_menu_npc",
                "nd_kick_hero_menu",
                "nd_kick_hero_menu",
                "{=nd_kick_hero_menu_npc}Wait, are you serious?", //they ask
                null,
                null,
                100,
                null
            );
            game.AddPlayerLine(
                "nd_kick_hero_yes",
                "nd_kick_hero_menu",
                "nd_kick_hero_menu_confirm",
                "{=nd_kick_hero_yes}Yes, I'm serious.",
                null, null, 100, null, null
            );
            game.AddPlayerLine(
                "nd_kick_hero_no",
                "nd_kick_hero_menu",
                "start",
                "{=nd_kick_hero_no}Nah, I'm just joking.",
                null, null, 100, null, null
            );

            game.AddDialogLine(
                "nd_kick_hero_menu_confirm",
                "nd_kick_hero_menu_confirm",
                "start",
                "{=nd_kick_hero_menu_confirm}Very well, if that is your wish then be it. Farewell.", //they ask
                null,
                new ConversationSentence.OnConsequenceDelegate(() =>
                {
                    var hero = Hero.OneToOneConversationHero;

                    DecideDeparture(hero, hero.Clan, true);

                }),
                100,
                null
            );
        }


        public override void SyncData(IDataStore dataStore) { }

        //Parse values
        /// <summary>
        /// Parses a comma‑separated list of key:value pairs, ignoring extra spaces.
        /// e.g. "empire: 0.1,  aserai : -0.1" → { ["empire"]=0.1, ["aserai"]=-0.1 }.
        /// </summary>
        private Dictionary<string, double> ParseLenientMap(string raw)
        {
            var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw))
                return dict;

            // 1) split on commas
            var parts = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                // 2) find the first colon
                var idx = part.IndexOf(':');
                if (idx < 0)
                    continue; // no colon, skip

                // 3) trim key and value
                var key = part.Substring(0, idx).Trim();
                var value = part.Substring(idx + 1).Trim();

                if (key.Length == 0)
                    continue;

                // 4) parse the number
                if (double.TryParse(value, out var d))
                    dict[key] = d;
            }

            return dict;
        }

        private Dictionary<string, string> ParseLenientStringMap(string raw)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw))
                return dict;

            // 1) split on commas
            var parts = raw
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                // 2) locate the first colon
                var idx = part.IndexOf(':');
                if (idx < 0)
                    continue; // skip entries without a colon

                // 3) extract and trim key and value
                var key = part.Substring(0, idx).Trim();
                var value = part.Substring(idx + 1).Trim();

                if (key.Length == 0)
                    continue; // skip empty keys

                dict[key] = value;
            }

            return dict;
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            var s = NobleDepartureSettings.Instance;
            day = Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow;
            traitMods = ParseLenientMap(s.TraitModifiersJson);
            chanceMap = ParseLenientMap(s.CultureLeaveChanceJson);
            joinMap = ParseLenientMap(s.CultureJoinChanceJson);
            createMap = ParseLenientMap(s.CultureCreateChanceJson);
            wanderMap = ParseLenientMap(s.CultureWanderChanceJson);
            clanSuffixes = s.ClanNameSuffixesCsv.Split(',')
                .Select(c => c.Trim()).Where(c => c.Length > 0).ToList();
            kingdomMap = ParseLenientStringMap(s.KingdomNameSuffixesJson);

            clanSuffixes = clanSuffixes.Distinct().ToList();
            
            // Display key settings
            if (s.StatInfo)
            {
                InformationManager.DisplayMessage(new InformationMessage("NobleDeparture settings:", Colors.Cyan));
                InformationManager.DisplayMessage(new InformationMessage($"DepartureIntervalDays: {s.DepartureIntervalDays}"));
                InformationManager.DisplayMessage(new InformationMessage($"LeaveBaseProbability: {s.LeaveBaseProbability}"));
                InformationManager.DisplayMessage(new InformationMessage($"RelationFactor: {s.RelationFactor}"));
                InformationManager.DisplayMessage(new InformationMessage($"RelationLeave: {s.RelationLeave}"));
                InformationManager.DisplayMessage(new InformationMessage($"RelationLoss: {s.RelationLoss}"));
                InformationManager.DisplayMessage(new InformationMessage($"RelationGain: {s.RelationGain}"));
                InformationManager.DisplayMessage(new InformationMessage($"JoinExistingWeight: {s.JoinExistingWeight}"));
                InformationManager.DisplayMessage(new InformationMessage($"CreateNewWeight: {s.CreateNewWeight}"));
                InformationManager.DisplayMessage(new InformationMessage($"WandererWeight: {s.WandererWeight}"));
                InformationManager.DisplayMessage(new InformationMessage($"IncludeSpouse: {s.IncludeSpouse}"));
                InformationManager.DisplayMessage(new InformationMessage($"IncludeChildren: {s.IncludeChildren}"));
                InformationManager.DisplayMessage(new InformationMessage($"FamilyRelation: {s.FamilyRelation}"));
                InformationManager.DisplayMessage(new InformationMessage($"MaxActiveClans: {s.MaxActiveClans}"));
                InformationManager.DisplayMessage(new InformationMessage($"MaxActiveWanderers: {s.MaxActiveWanderers}"));
                InformationManager.DisplayMessage(new InformationMessage($"AllowWanderer: {s.AllowWanderer}"));
                InformationManager.DisplayMessage(new InformationMessage($"AllowPlayerClanDeparture: {s.AllowPlayerClanMembersDeparture}"));
                InformationManager.DisplayMessage(new InformationMessage($"AllowJoinPlayerClan: {s.AllowJoinPlayerClan}"));
                InformationManager.DisplayMessage(new InformationMessage($"FiefsAmount: {s.FiefsAmount}"));
                InformationManager.DisplayMessage(new InformationMessage($"MaxRenown: {s.MaxRenown}"));
                InformationManager.DisplayMessage(new InformationMessage($"MinRenown: {s.MinRenown}"));
                InformationManager.DisplayMessage(new InformationMessage($"MaxGold: {s.MaxGold}"));
                InformationManager.DisplayMessage(new InformationMessage($"MinGold: {s.MinGold}"));
                InformationManager.DisplayMessage(new InformationMessage($"Debug: {s.Debug}"));
                InformationManager.DisplayMessage(new InformationMessage($"InformAboutChanges: {s.Inform}"));
                InformationManager.DisplayMessage(new InformationMessage($"InformAboutPlayerChanges: {s.InformPlayer}"));
                InformationManager.DisplayMessage(new InformationMessage($"WandererCreate: {s.WandererCreate}"));
                InformationManager.DisplayMessage(new InformationMessage($"WandererCreateNumber: {s.WandererCreateNumber}"));
                InformationManager.DisplayMessage(new InformationMessage($"WandererCreateProbability: {s.WandererCreateProbability}"));
                InformationManager.DisplayMessage(new InformationMessage($"CreateKingdom: {s.CreateKingdom}"));

                InformationManager.DisplayMessage(new InformationMessage(
                    "KingdomNameSuffixes: " +
                    string.Join(", ", kingdomMap.Select(kv => $"{kv.Key}={kv.Value:F2}"))
                ));

                InformationManager.DisplayMessage(new InformationMessage(
                    "ClanNameSuffixes: " +
                    string.Join(", ", clanSuffixes)
                ));

                InformationManager.DisplayMessage(new InformationMessage(
                    "CultureProbabilityChance: " +
                    string.Join(", ", chanceMap.Select(kv => $"{kv.Key}={kv.Value:F2}"))
                ));
                InformationManager.DisplayMessage(new InformationMessage(
                    "CultureProbabilityJoin:   " +
                    string.Join(", ", joinMap.Select(kv => $"{kv.Key}={kv.Value:F2}"))
                ));
                InformationManager.DisplayMessage(new InformationMessage(
                    "CultureProbabilityCreate: " +
                    string.Join(", ", createMap.Select(kv => $"{kv.Key}={kv.Value:F2}"))
                ));
                InformationManager.DisplayMessage(new InformationMessage(
                    "CultureProbabilityWanderer: " +
                    string.Join(", ", wanderMap.Select(kv => $"{kv.Key}={kv.Value:F2}"))
                ));

                InformationManager.DisplayMessage(new InformationMessage("TraitModifiers: " +
                    string.Join(", ", traitMods.Select(kv => $"{kv.Key}={kv.Value:F2}"))
                ));
            }

        }

        private void OnDailyTick()
        {
            var s = NobleDepartureSettings.Instance;
            day++;
            if (((int)day) % s.DepartureIntervalDays != 0)
                return;  // skip until the next interval
            try
            {
                if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"NobleDeparture onDailyTick works"));

                traitMods = ParseLenientMap(s.TraitModifiersJson);
                chanceMap = ParseLenientMap(s.CultureLeaveChanceJson);
                joinMap = ParseLenientMap(s.CultureJoinChanceJson);
                createMap = ParseLenientMap(s.CultureCreateChanceJson);
                wanderMap = ParseLenientMap(s.CultureWanderChanceJson);
                clanSuffixes = s.ClanNameSuffixesCsv.Split(',')
                    .Select(c => c.Trim()).Where(c => c.Length > 0).ToList();
                kingdomMap = ParseLenientStringMap(s.KingdomNameSuffixesJson);

                clanSuffixes = clanSuffixes.Distinct().ToList();

                var clans = Clan.All
                    .Where(c=> !c.IsEliminated && c.Leader != null && !c.IsBanditFaction)
                    .Where(c=> s.AllowPlayerClanMembersDeparture || c != Clan.PlayerClan)
                    .ToList();
                foreach (var clan in clans)
                {

                    // Snapshot copy
                    var members = clan.Heroes
                        .Where(h => h != clan.Leader && h.IsAlive && h.IsActive && h.PartyBelongedToAsPrisoner == null && !h.IsChild && h.GetRelation(clan.Leader) <= s.RelationLeave && EligibleHeroes(h, clan))
                        .Where(h=> CanTransferHero(h))
                        .ToList();
                    foreach (var hero in members)
                    {
                        if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"Goes to process"));

                        TryProcessDeparture(hero, clan);

                        if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"End of process"));
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_fatalErrorLogged)
                {
                    _fatalErrorLogged = true;
                    InformationManager.DisplayMessage(new InformationMessage($"[NobleDeparture] DailyTick error: {ex.Message}"));
                }
            }

            if (s.WandererCreate)
            {
                // Use the same create‐weight you already compute in DecideDeparture

                var wCreateProb = s.WandererCreateProbability;

                if (Clan.All.Count(c => !c.IsEliminated) >= s.MaxActiveClans) wCreateProb = 0;

                if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"[WandererCreate] (create threshold {wCreateProb:F2})"));

                // only proceed if roll falls under 'create'
                if (MBRandom.RandomFloat <= wCreateProb)
                {
                    TryCreateWandererClan();
                }
            }
        }

        private bool EligibleHeroes(Hero hero, Clan clan)
        {
            var s = NobleDepartureSettings.Instance;
            if (s == null || hero == null || clan == null) return false;
            if (!s.AllowWanderer && hero.IsWanderer) return false;
            if (!s.AllowCrown && clan.Kingdom != null)
            {
                var kingdom = clan.Kingdom;
                if(kingdom.Leader == clan.Leader && 
                    (clan.Leader == hero.Father || clan.Leader == hero.Mother) ) return false;
            }
            if(!s.AllowLeadChild && clan.Leader.Children.Contains(hero) ) return false;

            return true;
        }

        private void TryCreateWandererClan()
        {
            var s = NobleDepartureSettings.Instance;
            // 1) grab up to N random clanless wanderers
            var wanderers = Hero.AllAliveHeroes
                .Where(h => h.IsWanderer && h.Clan == null && h.IsActive && h.PartyBelongedTo == null && h.PartyBelongedToAsPrisoner == null)
                .OrderBy(_ => MBRandom.RandomFloat)
                .Take(s.WandererCreateNumber)
                .ToList();
            if (wanderers.Count < s.WandererCreateNumber) return;

            // 2) pick one as the “founder”
            int idx = MBRandom.RandomInt(wanderers.Count);
            var founder = wanderers[idx];

            // 4) set occupation of all wanderers in this list

            foreach (var w in wanderers)
            {
                if(s.WandNameChange)
                w.SetName(w.FirstName, w.FirstName);

                CleanupHeroRoles(w);
                w.SetNewOccupation(Occupation.Lord);
            }

            List<Hero> spouses = null;

            if(s.IncludeSpouse)
            spouses = Hero.AllAliveHeroes.Where(h => h == founder.Spouse).ToList();

            // 5) **use your existing CreateNewClan** (it takes hero+oldClan)
            CreateNewClan(founder, /*oldClan*/ null, spouses);
            var nc = founder.Clan;

            if (s.Debug)
                InformationManager.DisplayMessage(
                    new InformationMessage($"[WandererCreate] {founder.Name} founded a clan with {wanderers.Count} other wanderers"));






            // 6) assign the rest into that clan
            foreach (var w in wanderers)
            {
                if(s.WandererNoble)
                GiveNobleKit(w);

                if (w == founder) continue;
                w.Clan = nc;
                w.UpdateHomeSettlement();
            }

        }


        private void TryProcessDeparture(Hero hero, Clan origClan)
        {
            var s = NobleDepartureSettings.Instance;
            float rel = hero.GetRelation(origClan.Leader);
            double chance = s.LeaveBaseProbability + ((1.0 - rel / 100.0) * s.RelationFactor);

            if(traitMods == null)
            traitMods = ParseLenientMap(s.TraitModifiersJson);

            foreach (var kv in traitMods)
            {
                var trait = MBObjectManager.Instance.GetObject<TraitObject>(kv.Key);
                
                if (trait != null)
                {
                    if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"Hero Trait: {hero.GetTraitLevel(trait)} "));

                    chance += hero.GetTraitLevel(trait) * kv.Value;
                }

                // culture modifier
                var cultureId = hero.Culture.StringId.ToLowerInvariant();

                if (chanceMap == null)
                    chanceMap = ParseLenientMap(s.CultureLeaveChanceJson);

                if (chanceMap.TryGetValue(cultureId, out var cultChance))
                {
                    chance += cultChance;
                    if (s.Debug)
                        InformationManager.DisplayMessage(
                            new InformationMessage($"[ND-DEBUG] Culture '{cultureId}' modifier: {cultChance}")
                        );
                }


            }
            if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"Chance for departure {chance.ToString()} "));
            if (MBRandom.RandomFloat <= chance) // MBRandom.RandomFloat (0.0 to 1.0)
                DecideDeparture(hero, origClan, false);
        }

        private void DecideDeparture(Hero hero, Clan origClan, bool noReturn)
        {
            var s = NobleDepartureSettings.Instance;
            // Decide next step
            float wCreate = (float)s.CreateNewWeight;
            float wWanderer = (float)s.WandererWeight;
            float wJoin = (float)s.JoinExistingWeight;

            var cultureId = hero.Culture.StringId.ToLowerInvariant();

            if (joinMap == null)
                joinMap = ParseLenientMap(s.CultureJoinChanceJson);
            if (createMap == null)
                createMap = ParseLenientMap(s.CultureCreateChanceJson);
            if (wanderMap == null)
                wanderMap = ParseLenientMap(s.CultureWanderChanceJson);

            if (wJoin > 0 && joinMap.TryGetValue(cultureId, out var cj)) wJoin += (float)cj;
            if (wCreate > 0 && createMap.TryGetValue(cultureId, out var cc)) wCreate += (float)cc;
            if (wWanderer > 0 && wanderMap.TryGetValue(cultureId, out var cw)) wWanderer += (float)cw;

            if (Clan.All.Count(c => !c.IsEliminated) >= s.MaxActiveClans) wCreate = 0;
            if (Hero.AllAliveHeroes.Count(c => c.Occupation == Occupation.Wanderer) >= s.MaxActiveWanderers) wWanderer = 0;


            float total = wCreate + wWanderer + wJoin;
            float roll = MBRandom.RandomFloat * total;
            if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"Roll : {roll}"));

            List<Hero> spouses = null;

            if (s.IncludeSpouse)
                spouses = Hero.AllAliveHeroes.Where(h => h == hero.Spouse).ToList();


            if (roll < wCreate)
            {
                if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"Create A NEW CLAN"));
                ProcessDeparture(hero, origClan, spouses);
                CreateNewClan(hero, origClan, spouses);
            }

            else if (roll < wWanderer + wCreate)
            {
                if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"Become Wanderer"));
                ProcessDeparture(hero, origClan, spouses);
                MakeWanderer(hero, spouses);
            }

            else if (roll < wJoin + wWanderer + wCreate || noReturn)
            {
                if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"Join Clan"));
                ProcessDeparture(hero, origClan, spouses);
                TryJoinExisting(hero, spouses);
            }

            else return;

        }


        private void ProcessDeparture(Hero hero, Clan origClan, List<Hero> spousesList)
        {
            var s = NobleDepartureSettings.Instance;
            // Remove any issues/quests where this hero is the issuer
            try
            {
                var issueManager = Campaign.Current.IssueManager;
                var toRemove = issueManager.Issues
                    .Where(kv => kv.Key == hero || kv.Value.IssueOwner == hero)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var owner in toRemove)
                    owner.Issue.CompleteIssueWithCancel();

                var qm = Campaign.Current.QuestManager;
                // Get all quests where this hero is the quest‐giver
                var issued = qm.GetQuestGiverQuests(hero).ToList();
                foreach (var quest in issued)
                {
                    // Complete them as cancel (you can also choose Success, Timeout, etc.)
                    quest.CompleteQuestWithCancel();
                    if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"[ND-DEBUG] Cancelled quest {quest} for {hero.Name}"));
                }
            }
            catch (Exception) { /* ignore if something goes wrong */ }



            CleanupHeroRoles(hero);

            if (s.Inform && origClan != Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CLANLEFT}{HERO} left {CLANNAME}.")
                .SetTextVariable("HERO", hero.Name)
                .SetTextVariable("CLANNAME", origClan.Name)
                .ToString(), Colors.Gray));

            if (s.InformPlayer && origClan == Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CLANLEFT_PLAYER}{HERO} left your clan.")
                .SetTextVariable("HERO", hero.Name)
                .ToString() , Colors.Red));

            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, origClan.Leader, s.RelationLoss, origClan != Clan.PlayerClan ? s.Inform : s.InformPlayer);


            // Spouse
            if (s.IncludeSpouse && spousesList != null)
            {
                var spouses = spousesList
                    .Where(w => w == hero.Spouse && w.Clan == origClan && !w.IsClanLeader && !w.IsKingdomLeader && w != Hero.MainHero && s.FamilyRelation <= hero.GetRelation(w))
                    .Where(w =>
                    {
                        var party = w.PartyBelongedTo;

                        if (party != null)
                        {
                            if (!party.IsActive || party.MapEvent != null || party.SiegeEvent != null)
                            {
                                return false;
                            }
                        }

                        return true;
                    })
                    .ToList();
                foreach (var spouse in spouses)
                {
                    if (!s.AllowWanderer && spouse.Occupation == Occupation.Wanderer) continue;
                    CleanupHeroRoles(spouse);

                    if (s.Inform && origClan != Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CLANLEFT_SPOUSE}{HERO}'s spouse, {HERO_SPOUSE} left {CLANNAME}.")
                        .SetTextVariable("HERO", hero.Name)
                        .SetTextVariable("HERO_SPOUSE", spouse.Name)
                        .SetTextVariable("CLANNAME", origClan.Name)
                        .ToString(), Colors.Gray));

                    if (s.InformPlayer && origClan == Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CLANLEFT_PLAYER_SPOUSE}{HERO}'s spouse, {HERO_SPOUSE} left your clan.")
                        .SetTextVariable("HERO", hero.Name)
                        .SetTextVariable("HERO_SPOUSE", spouse.Name)
                        .ToString(), Colors.Red));


                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(spouse, origClan.Leader, s.RelationLoss, origClan != Clan.PlayerClan ? s.Inform : s.InformPlayer);
                }

            }

            // Children
            if (s.IncludeChildren)
            {
                var children = hero.Children
                    .Where(c => c.IsAlive && !c.IsKingdomLeader && c != Hero.MainHero && c.Clan == origClan && !c.IsClanLeader && c.GetRelation(hero) >= s.FamilyRelation)
                    .Where(c =>
                    {
                        var party = c.PartyBelongedTo;

                        if (party != null)
                        {
                            if (!party.IsActive || party.MapEvent != null || party.SiegeEvent != null)
                            {
                                return false;
                            }
                        }

                        return true;
                    })
                    .ToList();
                foreach (var kid in children)
                {
                    if (!s.AllowWanderer && kid.Occupation == Occupation.Wanderer) continue;
                    CleanupHeroRoles(kid);

                    if (s.Inform && origClan != Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CLANLEFT_CHILD}{HERO}'s child, {HERO_CHILD} left {CLANNAME}.")
                        .SetTextVariable("HERO", hero.Name)
                        .SetTextVariable("HERO_CHILD", kid.Name)
                        .SetTextVariable("CLANNAME", origClan.Name)
                        .ToString(), Colors.Gray));

                    if (s.InformPlayer && origClan == Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CLANLEFT_PLAYER_CHILD}{HERO}'s child, {HERO_CHILD} left your clan.")
                        .SetTextVariable("HERO", hero.Name)
                        .SetTextVariable("HERO_CHILD", kid.Name)
                        .ToString(), Colors.Red));

                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(kid, origClan.Leader, s.RelationLoss, origClan != Clan.PlayerClan ? s.Inform : s.InformPlayer);


                }
            }


        }

        private void ApplyJoin(Hero hero, Clan newClan, List<Hero> spousesList)
        {
            var s = NobleDepartureSettings.Instance;
            if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"Clan Joined Succesfully"));
            hero.Clan = newClan;
            hero.UpdateHomeSettlement();

            // Inform messages
            if (s.Inform && newClan != Clan.PlayerClan)
                InformationManager.DisplayMessage(
                  new InformationMessage(new TextObject("{=NobleD_CLANJOIN}{HERO} joined {CLANNAME}.")
                    .SetTextVariable("HERO", hero.Name)
                    .SetTextVariable("CLANNAME", newClan.Name)
                    .ToString(), Colors.Gray));

            if (s.InformPlayer && newClan == Clan.PlayerClan)
                InformationManager.DisplayMessage(
                  new InformationMessage(new TextObject("{=NobleD_CLANJOIN_PLAYER}{HERO} joined your clan.")
                    .SetTextVariable("HERO", hero.Name)
                    .ToString(),
                    Colors.Green));

            // Relation change
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, newClan.Leader, s.RelationGain, newClan != Clan.PlayerClan ? s.Inform : s.InformPlayer);

            // Spouse and children—same pattern, but all inside here...
            if (s.IncludeSpouse && spousesList != null)
            {
                var spouses = spousesList
                    .Where(w => w == hero.Spouse && w.Clan == null && !w.IsClanLeader && !w.IsKingdomLeader && w != Hero.MainHero && s.FamilyRelation <= hero.GetRelation(w))
                    .Where(h =>
                    {
                        var party = h.PartyBelongedTo;

                        if (party != null)
                        {
                            if (!party.IsActive || party.MapEvent != null || party.SiegeEvent != null)
                            {
                                return false;
                            }
                        }

                        return true;
                    })
                    .ToList();
                foreach (var spouse in spouses)
                {
                    if (!s.AllowWanderer && spouse.Occupation == Occupation.Wanderer) continue;
                    spouse.Clan = hero.Clan;
                    spouse.UpdateHomeSettlement();

                    if (s.Inform && hero.Clan != Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CLANJOIN_SPOUSE}{HERO}'s spouse, {HERO_SPOUSE} joined {CLANNAME}.")
                        .SetTextVariable("HERO", hero.Name)
                        .SetTextVariable("HERO_SPOUSE", spouse.Name)
                        .SetTextVariable("CLANNAME", hero.Clan.Name)
                        .ToString(), Colors.Gray));

                    if (s.InformPlayer && hero.Clan == Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CLANJOIN_PLAYER_SPOUSE}{HERO}'s spouse, {HERO_SPOUSE} joined your clan.")
                        .SetTextVariable("HERO", hero.Name)
                        .SetTextVariable("HERO_SPOUSE", spouse.Name)
                        .ToString(), Colors.Green));

                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(spouse, hero.Clan.Leader, s.RelationGain, hero.Clan != Clan.PlayerClan ? s.Inform : s.InformPlayer);
                }

            }

            if (s.IncludeChildren)
            {
                var children = hero.Children
                    .Where(c => c.IsAlive && !c.IsKingdomLeader && c != Hero.MainHero && c.Clan == null && !c.IsClanLeader && c.GetRelation(hero) >= s.FamilyRelation)
                    .Where(h =>
                    {
                        var party = h.PartyBelongedTo;

                        if (party != null)
                        {
                            if (!party.IsActive || party.MapEvent != null || party.SiegeEvent != null)
                            {
                                return false;
                            }
                        }

                        return true;
                    })
                    .ToList();
                foreach (var kid in children)
                {
                    if (!s.AllowWanderer && kid.Occupation == Occupation.Wanderer) continue;
                    kid.Clan = hero.Clan;
                    kid.UpdateHomeSettlement();
                    if (s.Inform && hero.Clan != Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CLANJOIN_CHILD}{HERO}'s child, {HERO_CHILD} joined {CLANNAME}.")
                        .SetTextVariable("HERO", hero.Name)
                        .SetTextVariable("HERO_CHILD", kid.Name)
                        .SetTextVariable("CLANNAME", hero.Clan.Name)
                        .ToString(), Colors.Gray));

                    if (s.InformPlayer && hero.Clan == Clan.PlayerClan) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CLANJOIN_PLAYER_CHILD}{HERO}'s child, {HERO_CHILD} joined your clan.")
                        .SetTextVariable("HERO", hero.Name)
                        .SetTextVariable("HERO_CHILD", kid.Name)
                        .ToString(), Colors.Green));

                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(kid, hero.Clan.Leader, s.RelationGain, hero.Clan != Clan.PlayerClan ? s.Inform : s.InformPlayer);


                }

            }

            if (s.EnableDivorce)
            {
                var spouses = Hero.AllAliveHeroes.Where(w => w == hero.Spouse && (w.Clan != hero.Clan || w.IsWanderer));
                foreach (var spouse in spouses)
                {
                    if (spouse.Spouse == hero)
                        spouse.Spouse = null;
                    if (hero.Spouse == spouse)
                        hero.Spouse = null;

                    if (s.Inform) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_DIVORCE}{HERO_SPOUSE} divorced {HERO}.")
                        .SetTextVariable("HERO", hero.Name)
                        .SetTextVariable("HERO_SPOUSE", spouse.Name)
                        .ToString(), Colors.Gray));
                }
            }
        }


        private void TryJoinExisting(Hero hero, List<Hero> spouses)
        {
            var s = NobleDepartureSettings.Instance;
            if (s.Debug) InformationManager.DisplayMessage(new InformationMessage($"Clan Trying Join"));
            var list = Clan.All
                         .Where(c => !c.IsEliminated && c != hero.Clan && !c.IsBanditFaction)
                         .Where(c => s.AllowJoinPlayerClan || c != Clan.PlayerClan)
                         .OrderByDescending(c => hero.GetRelation(c.Leader))
                         .Take(5).ToList();

            if (list.Count > 0)
            {
                
                
                var pick = list[MBRandom.RandomInt(list.Count)];



                    // if it’s not the player clan, just join immediately
                    if (pick != Clan.PlayerClan)
                {
                    ApplyJoin(hero, pick, spouses);
                    return;
                }

                // otherwise—ask the player
                Campaign.Current.SetTimeSpeed(0);
                var title = new TextObject("{=NobleD_JOINPLAYER_TITLE}{HERO} requests to join your clan")
                  .SetTextVariable("HERO", hero.Name);
                var body = new TextObject("{=NobleD_JOINPLAYER_BODY}{HERO} has left their clan and requests to join yours. Accept?")
                  .SetTextVariable("HERO", hero.Name);

                InformationManager.ShowInquiry(new InquiryData(
                  title.ToString(),
                  body.ToString(),
                  true, true,
                  GameTexts.FindText("str_yes").ToString(),
                  GameTexts.FindText("str_no").ToString(),

                  // YES: player accepted
                  () => {
                      ApplyJoin(hero, Clan.PlayerClan, spouses);
                  },

                  // NO: player declined → pick a non-player clan and still join
                  () => {
                      if (list.Count == 1 && pick == Clan.PlayerClan)
                      {
                          MakeWanderer(hero, spouses);
                          return;
                      }
                      Clan fallback;
                      do
                      {
                          fallback = list[MBRandom.RandomInt(list.Count)];
                      } while (fallback == Clan.PlayerClan);
                      ApplyJoin(hero, fallback, spouses);
                  }
                ), true);
            }
            else
            {
                if(Clan.All.Count(c => !c.IsEliminated) <= s.MaxActiveClans)
                {
                    CreateNewClan(hero, null, spouses);
                }

                else
                {
                    MakeWanderer(hero, spouses);
                }



            }
        }

        private void CreateNewClan(Hero hero, Clan oldClan, List<Hero> spousesList)
        {
            var s = NobleDepartureSettings.Instance;
            if (hero.Occupation != Occupation.Lord) hero.SetNewOccupation(Occupation.Lord);
            var id = $"NobleD_{hero.StringId}_clan_{MBRandom.RandomInt(1000000)}";
            while (Clan.All.Any(c => c.StringId == id)) id = $"NobleD_{hero.StringId}_clan_{MBRandom.RandomInt(1000000)}";

            var nc = Clan.CreateClan(id);

            if(clanSuffixes == null)
            {
                clanSuffixes = s.ClanNameSuffixesCsv.Split(',')
                .Select(c => c.Trim()).Where(c => c.Length > 0).ToList();
            }
            
            var crname = clanSuffixes[MBRandom.RandomInt(clanSuffixes.Count)];
            var rlname = new TextObject("{=NobleD_" + crname + "}" + crname);
            var nm = new TextObject("{=NobleD_NEWCLANNAME}{HERO} {NAME}")
                .SetTextVariable("HERO", hero.Name)
                .SetTextVariable("NAME", rlname);
            var bn = Banner.CreateRandomClanBanner(-1);
            bn.ChangeBackgroundColor(GetRandomBannerColor(), GetRandomBannerColor());

            nc.InitializeClan(nm, nm, hero.Culture, bn);
            nc.SetLeader(hero);
            nc.AddRenown(MBRandom.RandomInt(s.MinRenown, s.MaxRenown));

            // determine noble vs mercenary by blood relation
            if(oldClan == null && s.WandererWay != 2)
            {
                bool isNoble = GetBloodRelatives(hero)
                    .Any(r => r != hero && r.Clan != null && r.Occupation == Occupation.Lord && !r.Clan.IsMinorFaction);

                if ((isNoble == false && s.WandererWay == 0) || s.WandererWay == 1)
                {

                    nc.GetType().GetProperty("IsMinorFaction", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(nc, true);
                    nc.GetType().GetProperty("IsClanTypeMercenary", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(nc, true);


                    if (s.Debug)
                        InformationManager.DisplayMessage(
                            new InformationMessage($"[NobleDeparture] Clan is Mercenary"));

                }
            }
            else if(oldClan != null && s.CreateWay != 2)
            {
                bool isNoble = GetBloodRelatives(hero)
                    .Any(r => r != hero && r.Clan != null && r.Occupation == Occupation.Lord && !r.Clan.IsMinorFaction);

                if ((isNoble == false && s.CreateWay == 0) || s.CreateWay == 1)
                {

                    nc.GetType().GetProperty("IsMinorFaction", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(nc, true);
                    nc.GetType().GetProperty("IsClanTypeMercenary", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(nc, true);


                    if (s.Debug)
                        InformationManager.DisplayMessage(
                            new InformationMessage($"[NobleDeparture] Clan is Mercenary"));

                }
            }

            var settlements = oldClan?
            .Settlements
            .Where(f => !f.IsVillage)
            .ToList();


            if (s.FiefsAmount > 0 && oldClan != null && !nc.IsMinorFaction && settlements.Count() > 1)
            {
                var list = settlements.ToList();
                while (s.FiefsAmount > nc.Settlements.Count() && settlements.Count() > 1)
                    {
                        var settlement = list[MBRandom.RandomInt(list.Count)];
                        list.Remove(settlement);

                    ChangeOwnerOfSettlementAction.ApplyBySiege(hero, hero, settlement);
                    
                }
                if(oldClan.Kingdom == null)
                {
                    DeclareWarAction.ApplyByDefault(nc, oldClan);
                } else
                {
                    DeclareWarAction.ApplyByDefault(nc, oldClan.Kingdom);
                }
                    


            }
            
            hero.Clan = nc;
            hero.Gold = MBRandom.RandomInt(s.MinGold, s.MaxGold);
            hero.UpdateHomeSettlement();
            nc.UpdateHomeSettlement(hero.HomeSettlement);
            if (s.Inform) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CLANCREATE}{HERO} founded {CLANNAME}.")
                .SetTextVariable("HERO", hero.Name)
                .SetTextVariable("CLANNAME", nc.Name)
                .ToString(), Colors.Gray));

            if (s.IncludeSpouse && spousesList != null)
            {
                var spouses = spousesList
                    .Where(w => w == hero.Spouse && w.Clan == null && !w.IsClanLeader && !w.IsKingdomLeader && w != Hero.MainHero && s.FamilyRelation <= hero.GetRelation(w))
                    .Where(h =>
                    {
                        var party = h.PartyBelongedTo;

                        if (party != null)
                        {
                            if (!party.IsActive || party.MapEvent != null || party.SiegeEvent != null)
                            {
                                return false;
                            }
                        }

                        return true;
                    })
                    .ToList();
                foreach (var spouse in spouses)
                {

                    if (spouse.Occupation != Occupation.Lord) spouse.SetNewOccupation(Occupation.Lord);
                    spouse.Clan = hero.Clan;
                    spouse.UpdateHomeSettlement();
                    
                    if (s.Inform) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CLANCREATE_SPOUSE}{HERO_SPOUSE} has joined {HERO}'s clan named {CLANNAME}.")
                        .SetTextVariable("HERO", hero.Name)
                        .SetTextVariable("HERO_SPOUSE", spouse.Name)
                        .SetTextVariable("CLANNAME", nc.Name)
                        .ToString(), Colors.Gray));
                }


            }

            if (s.IncludeChildren)
            {
                var children = hero.Children
                    .Where(c => c.IsAlive && !c.IsKingdomLeader && c != Hero.MainHero && c.Clan == null && !c.IsClanLeader && c.GetRelation(hero) >= s.FamilyRelation)
                    .Where(h =>
                    {
                        var party = h.PartyBelongedTo;

                        if (party != null)
                        {
                            if (!party.IsActive || party.MapEvent != null || party.SiegeEvent != null)
                            {
                                return false;
                            }
                        }

                        return true;
                    })
                    .ToList();
                foreach (var kid in children)
                {

                    if (kid.Occupation != Occupation.Lord) kid.SetNewOccupation(Occupation.Lord);
                        kid.Clan = hero.Clan;
                        kid.UpdateHomeSettlement();
                        if (s.Inform) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CLANCREATE_CHILD}{HERO_CHILD} has joined {HERO}'s clan named {CLANNAME}.")
                            .SetTextVariable("HERO", hero.Name)
                            .SetTextVariable("HERO_CHILD", kid.Name)
                            .SetTextVariable("CLANNAME", nc.Name)
                            .ToString(), Colors.Gray));

                }

            }

            if(s.EnableDivorce)
            {
                var spouses = Hero.AllAliveHeroes.Where(w => w == hero.Spouse && (w.Clan != hero.Clan || w.IsWanderer));
                foreach (var spouse in spouses)
                {
                    if (spouse.Spouse == hero)
                        spouse.Spouse = null;
                    if (hero.Spouse == spouse)
                        hero.Spouse = null;

                    if (s.Inform) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_DIVORCE}{HERO_SPOUSE} divorced {HERO}.")
                        .SetTextVariable("HERO", hero.Name)
                        .SetTextVariable("HERO_SPOUSE", spouse.Name)
                        .ToString(), Colors.Gray));
                }
            }

            if (hero.Clan.IsMinorFaction || hero.Clan.IsClanTypeMercenary) return;
            CreateOrJoin(hero, hero.Clan, oldClan);
        }

        private void MakeWanderer(Hero hero, List<Hero> spousesList)
        {
            var s = NobleDepartureSettings.Instance;
            TextObject newName = new TextObject("{=28tWEFNi}{FIRSTNAME} the Wanderer");
            if (hero.Occupation == Occupation.Lord)
            {
                if (s.Inform) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_WANDERER}{HERO} is now a wanderer.")
                    .SetTextVariable("HERO", hero.Name)
                    .ToString(), Colors.Gray));
                hero.SetNewOccupation(Occupation.Wanderer);
                newName.SetTextVariable("FIRSTNAME", hero.FirstName);

                if (s.NobleNameChange)
                    hero.SetName(newName, hero.FirstName);

                hero.UpdateHomeSettlement();
            }

            if (s.IncludeSpouse && spousesList != null)
            {
                var spouses = spousesList
                    .Where(w => w == hero.Spouse && w.Occupation == Occupation.Lord && w.Clan == null && !w.IsClanLeader && !w.IsKingdomLeader && w != Hero.MainHero && s.FamilyRelation <= hero.GetRelation(w))
                    .Where(h =>
                    {
                        var party = h.PartyBelongedTo;

                        if (party != null)
                        {
                            if (!party.IsActive || party.MapEvent != null || party.SiegeEvent != null)
                            {
                                return false;
                            }
                        }

                        return true;
                    })
                    .ToList();
                foreach (var spouse in spouses)
                {
                    TextObject newNameSpouse = new TextObject("{=28tWEFNi}{FIRSTNAME} the Wanderer");
                    if (s.Inform) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_SPOUSE_WANDERER}{HERO}'s spouse, {HERO_SPOUSE} is now a wanderer.")
                    .SetTextVariable("HERO", hero.Name)
                    .SetTextVariable("HERO_SPOUSE", spouse.Name)
                    .ToString(), Colors.Gray));

                    spouse.SetNewOccupation(Occupation.Wanderer);
                    newNameSpouse.SetTextVariable("FIRSTNAME", spouse.FirstName);

                    if (s.NobleNameChange)
                        spouse.SetName(newNameSpouse, spouse.FirstName);
                    spouse.UpdateHomeSettlement();
                }
                

            }

            if (s.IncludeChildren)
            {
                var children = hero.Children
                    .Where(c => c.IsAlive && !c.IsKingdomLeader && c != Hero.MainHero && c.Clan == null && !c.IsClanLeader && c.Occupation == Occupation.Lord && c.GetRelation(hero) >= s.FamilyRelation)
                    .Where(h =>
                    {
                        var party = h.PartyBelongedTo;

                        if (party != null)
                        {
                            if (!party.IsActive || party.MapEvent != null || party.SiegeEvent != null)
                            {
                                return false;
                            }
                        }

                        return true;
                    })
                    .ToList();
                foreach (var kid in children)
                {
                    TextObject newNameKid = new TextObject("{=28tWEFNi}{FIRSTNAME} the Wanderer");
                    if (s.Inform) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_CHILD_WANDERER}{HERO}'s children, {HERO_CHILD} is now a wanderer.")
                            .SetTextVariable("HERO", hero.Name)
                            .SetTextVariable("HERO_CHILD", kid.Name)
                            .ToString(), Colors.Gray));
                        kid.SetNewOccupation(Occupation.Wanderer);
                    newNameKid.SetTextVariable("FIRSTNAME", kid.FirstName);

                    if (s.NobleNameChange)
                        kid.SetName(newNameKid, kid.FirstName);

                        kid.UpdateHomeSettlement();
                    
                        
                }
            }

            if (s.EnableDivorce)
            {
                var spouses = Hero.AllAliveHeroes.Where(w => w == hero.Spouse && (w.Clan != hero.Clan || w.IsWanderer) );
                foreach (var spouse in spouses)
                {
                    if (spouse.Spouse == hero || hero.Spouse == spouse)
                    {
                        spouse.Spouse = null;
                        hero.Spouse = null;
                    }


                    if (s.Inform) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_DIVORCE}{HERO_SPOUSE} divorced {HERO}.")
                        .SetTextVariable("HERO", hero.Name)
                        .SetTextVariable("HERO_SPOUSE", spouse.Name)
                        .ToString(), Colors.Gray));
                }
            }

        }

        private void DivorceAll()
        {
            var heroes = Hero.AllAliveHeroes.Where(h => h.Spouse != null && (h.IsWanderer || h.Spouse.Clan != h.Clan) ).ToList();
            foreach(var hero in heroes)
            {
                hero.Spouse.Spouse = null;
                hero.Spouse = null;
            }
        }

        private void CleanupHeroRoles(Hero hero)
        {
            // Unassign as governor
            if (hero.GovernorOf != null)
            {
                ChangeGovernorAction.RemoveGovernorOf(hero);
            }

            if (hero.PartyBelongedTo != null)
            {
                MobileParty party = hero.PartyBelongedTo;
                if (party.Army != null && party.Army.LeaderParty == party)
                {
                    DisbandArmyAction.ApplyByUnknownReason(party.Army);
                }
                party.Army = null;
                
                if (party.Party.IsActive && party.Party.LeaderHero == hero)
                {
                    DisbandPartyAction.StartDisband(party);
                    party.Party.SetCustomOwner(null);
                    DestroyPartyAction.Apply(null, party); // test
                }
                else if (party.IsActive)
                {
                    party.MemberRoster.RemoveTroop(hero.CharacterObject);
                }
            }



            if (hero.CompanionOf != null)
            {
                hero.CompanionOf = null;
            }

            if (hero.BornSettlement == null)
            {
                hero.BornSettlement = SettlementHelper.FindRandomSettlement((Settlement x) => x.IsTown);
            }

            hero.Clan = null;
        }

        public bool CanTransferHero(Hero hero)
        {
            if (hero.PartyBelongedTo != null && (hero.PartyBelongedTo.MapEvent != null || hero.PartyBelongedTo.SiegeEvent != null))
            {
                return false;
            }
            return true;
        }

        private IEnumerable<Hero> GetBloodRelatives(Hero root, int generationsUp = 2, int generationsDown = 2)
        {
            var relatives = new HashSet<Hero>();
            void AddAncestors(Hero h, int depth)
            {
                if (h == null || depth < 0) return;
                if (h.Father != null && relatives.Add(h.Father))
                    AddAncestors(h.Father, depth - 1);       // father’s line
                if (h.Mother != null && relatives.Add(h.Mother))
                    AddAncestors(h.Mother, depth - 1);       // mother’s line
                                                             // uncles/aunts
                if (h.Father?.Siblings != null)
                    foreach (var sib in h.Father.Siblings)
                        if (relatives.Add(sib)) { /* no further rec */ }
                if (h.Mother?.Siblings != null)
                    foreach (var sib in h.Mother.Siblings)
                        if (relatives.Add(sib)) { /* no further rec */ }
            }
            void AddDescendants(Hero h, int depth)
            {
                if (h == null || depth < 0) return;
                foreach (var child in h.Children)
                    if (relatives.Add(child))
                        AddDescendants(child, depth - 1);
            }

            // start with the hero themself
            relatives.Add(root);
            // go up
            AddAncestors(root, generationsUp);
            // go down
            AddDescendants(root, generationsDown);

            return relatives;
        }

        //noble kit
        public static void GiveNobleKit(Hero newLord)
        {
            // 1) Build our donor pools in priority order
            bool isOtherLord(Hero h) => h.IsLord && h != newLord && !h.IsChild;

            // 1a) Same‐culture, same‐gender
            var donors = Hero.AllAliveHeroes
                .Where(isOtherLord)
                .Where(h=> h.IsFemale == newLord.IsFemale)
                .Where(h => h.Culture == newLord.Culture)
                .ToList();

            // 1b) Same‐culture, any gender
            if (donors.Count == 0)
            {
                donors = Hero.AllAliveHeroes
                .Where(isOtherLord)
                .Where(h => h.Culture == newLord.Culture)
                .ToList();
            }

            // 1c) Any culture, same gender
            if (donors.Count == 0)
            {
                donors = Hero.AllAliveHeroes
                .Where(isOtherLord)
                .Where(h => h.IsFemale == newLord.IsFemale)
                .ToList();
            }

            // 1d) Anywhere: any surviving lord
            if (donors.Count == 0)
            {
                donors = Hero.AllAliveHeroes
                    .Where(isOtherLord)
                    .ToList();
            }

            // If we still have nobody, give up
            if (donors.Count == 0) return;

            // 2) Pick a random donor
            var donor = donors[MBRandom.RandomInt(donors.Count)];

            var sourceEquip = donor.BattleEquipment;
            var targetEquip = newLord.BattleEquipment;

            // 3) Overwrite each non‐empty slot
            for (int i = 0; i < Equipment.EquipmentSlotLength; i++)
            {
                var elem = sourceEquip[i];
                if (!elem.IsEmpty)
                {
                    targetEquip.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, elem);
                }
            }
        }

        private static uint GetRandomBannerColor()
        {
            Random rng = new Random();

            byte r = (byte)rng.Next(256);
            byte g = (byte)rng.Next(256);
            byte b = (byte)rng.Next(256);
            return (0xFFu << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
        }

        //Create Or Join

        private void CreateOrJoin(Hero hero, Clan newClan, Clan oldClan)
        {
            var s = NobleDepartureSettings.Instance;
            if (s.Debug) InformationManager.DisplayMessage(new InformationMessage(
                    "CreateOrJOin Launched."));
            // Only non‑minor clans get to join or create a kingdom
            if (newClan.IsMinorFaction) return;
            if (newClan.IsClanTypeMercenary) return;
            if (newClan.IsBanditFaction) return;

            // Roll once
            float roll = MBRandom.RandomFloat;

            // 1) Create kingdom
            if (roll < (float)s.CreateKingdom && newClan.Settlements.Count > 0)
            {
                var id = $"NobleD_{hero.StringId}_kingdom_{MBRandom.RandomInt(1000000)}";
                while (Kingdom.All.Any(c => c.StringId == id)) id = $"NobleD_{hero.StringId}_kingdom_{MBRandom.RandomInt(1000000)}";

                var kingdom = Kingdom.CreateKingdom(id);

                if(kingdomMap == null)
                {
                    kingdomMap = ParseLenientStringMap(s.KingdomNameSuffixesJson);
                }

                var date = CampaignTime.Now.GetYear;
                // pick a random pair
                var pair = kingdomMap
                    .OrderBy(_ => MBRandom.RandomFloat)
                    .First();
                var suffix = pair.Key;   // e.g. "Empire"
                var rlname = new TextObject("{=NobleD_" + suffix + "}" + suffix);
                InformationManager.DisplayMessage(
                  new InformationMessage(
                    rlname.ToString()
                  ));
                var crname = new TextObject("{=NobleD_NEWKINGDOMNAME}{HERO} {NAME}")
                    .SetTextVariable("HERO", hero.Name)
                    .SetTextVariable("NAME", rlname);
                var title = pair.Value; // e.g. "Emperor"
                var ruler = new TextObject("{=NobleD_NEWKINGDOMRULER}{NAME}")
                    .SetTextVariable("NAME", title);
                var capitals = newClan.Settlements.Where(c => !c.IsVillage).ToList();
                var capital = capitals.GetRandomElement();
                var text = new TextObject("{=NobleD_NEWKINGDOMTEXT}{NAME} was founded by {HERO} in {YEAR}.")
                    .SetTextVariable("HERO", hero.Name)
                    .SetTextVariable("NAME", crname)
                    .SetTextVariable("YEAR", date);

                newClan.UpdateHomeSettlement(capital);
                kingdom.InitializeKingdom(crname, crname, newClan.Culture, newClan.Banner, newClan.Color, newClan.Color2, capital, text, crname, ruler);
                kingdom.KingdomBudgetWallet = MBRandom.RandomInt(s.MinGold, s.MaxGold);
                ChangeKingdomAction.ApplyByCreateKingdom(newClan, kingdom);

                kingdom.RulingClan = newClan;

                // keep policies from the old clan kingdom
                foreach (var policy in oldClan?.Kingdom.ActivePolicies)
                {
                    kingdom.AddPolicy(policy);
                }

                if (oldClan?.Kingdom == null)
                {
                    DeclareWarAction.ApplyByDefault(kingdom, oldClan);
                }
                else
                {
                    DeclareWarAction.ApplyByDefault(kingdom, oldClan?.Kingdom);
                }


                if (s.Inform) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_NEWKINGDOM}{HERO} has founded a new kingdom.")
                    .SetTextVariable("HERO", hero.Name)
                    .ToString(), Colors.Gray));
            }
            // 2) Join kingdom
            else
            {

                var kingdoms = Kingdom.All
                    .Where(k => !k.IsEliminated && k.Settlements.Count > 0)
                    .OrderByDescending(k =>
                    {
                        var relation = hero.GetRelation(k.Leader);
                        var sizeBonus = k.Settlements.Count;
                        var cultureBonus = hero.Culture == k.Culture
                                              ? s.CultureJoinKingdom
                                              : 0f;
                        return relation
                             + sizeBonus
                             + cultureBonus;
                    })
                    .ToList();
                var best = kingdoms.FirstOrDefault();

                if (best != null && best != Hero.MainHero.Clan.Kingdom)
                {
                    // Use the campaign action to join a kingdom
                    ChangeKingdomAction.ApplyByJoinToKingdom(newClan, best);

                    if (s.Inform) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_JOINKINGDOM}{CLAN} has joined a {NAME}.")
                        .SetTextVariable("CLAN", newClan.Name)
                        .SetTextVariable("NAME", newClan.Kingdom.Name)
                        .ToString(), Colors.Gray));
                }

                if (best == Hero.MainHero.Clan.Kingdom && Hero.MainHero.IsKingdomLeader)
                {
                    // otherwise—ask the player
                    Campaign.Current.SetTimeSpeed(0);
                    var title = new TextObject("{=NobleD_JOINPLAYERKINGDOM_TITLE}{HERO} requests to join your kingdom")
                      .SetTextVariable("HERO", hero.Name);
                    var body = new TextObject("{=NobleD_JOINPLAYERKINGDOM_BODY}{HERO} has made new clan and requests to join your kingdom. Accept?")
                      .SetTextVariable("HERO", hero.Name);

                    InformationManager.ShowInquiry(new InquiryData(
                      title.ToString(),
                      body.ToString(),
                      true, true,
                      GameTexts.FindText("str_yes").ToString(),
                      GameTexts.FindText("str_no").ToString(),

                      // YES: player accepted
                      () => {
                          // Use the campaign action to join a kingdom
                          ChangeKingdomAction.ApplyByJoinToKingdom(newClan, best);

                          if (s.InformPlayer) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_JOINKINGDOM}{CLAN} has joined a {NAME}.")
                              .SetTextVariable("CLAN", newClan.Name)
                              .SetTextVariable("NAME", newClan.Kingdom.Name)
                              .ToString(), Colors.Gray));
                      },

                      // NO: player declined → pick a non-player clan and still join
                      () => {
                          if (kingdoms.Count <= 1 && best == Hero.MainHero.Clan.Kingdom)
                          {
                              return;
                          }
                          Kingdom fallback;
                          do
                          {
                              fallback = kingdoms[MBRandom.RandomInt(kingdoms.Count)];
                          } while (fallback == Hero.MainHero.Clan.Kingdom);
                          // Use the campaign action to join a kingdom
                          ChangeKingdomAction.ApplyByJoinToKingdom(newClan, fallback);

                          if (s.Inform) InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=NobleD_JOINKINGDOM}{CLAN} has joined a {NAME}.")
                              .SetTextVariable("CLAN", newClan.Name)
                              .SetTextVariable("NAME", newClan.Kingdom.Name)
                              .ToString(), Colors.Gray));
                      }
                    ), true);
                }

                
            }
            // 3) Otherwise, stays without a kingdom (remains minor/freelance)
        }

        // for console commands
        public void ForceDepartureCheck(Hero hero)
        {

            DecideDeparture(hero, hero.Clan, true);
        }

    }
}
