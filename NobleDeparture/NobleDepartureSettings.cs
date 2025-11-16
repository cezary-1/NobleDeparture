using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using System;
using TaleWorlds.Localization;

namespace NobleDeparture
{
    public sealed class NobleDepartureSettings : AttributeGlobalSettings<NobleDepartureSettings>
    {
        public override string Id => "NobleDeparture";
        public override string DisplayName => new TextObject("{=ND_MCM_TITLE}Noble Departure").ToString();
        public override string FolderName => "NobleDeparture";
        public override string FormatType => "json";

        //── General ─────────────────────────────────────────────────────────

        [SettingPropertyInteger(
            "{=ND_MCM_INTERVAL}Departure Interval (days)",
            1, 81, Order = 0, RequireRestart = false,
            HintText = "{=ND_MCM_INTERVAL_HINT}How often (in days) nobles are checked. (Default: 1)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General")]
        public int DepartureIntervalDays { get; set; } = 1;

        [SettingPropertyFloatingInteger(
            "{=ND_MCM_LEAVE}Base Leave Probability",
            0f, 1f, "#0%",
            Order = 1, RequireRestart = false,
            HintText = "{=ND_MCM_LEAVE_HINT}Base daily chance a noble considers leaving. (Default: 0.15)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General")]
        public float LeaveBaseProbability { get; set; } = 0.15f;

        [SettingPropertyFloatingInteger(
            "{=ND_MCM_RELFACTOR}Relation Factor",
            0f, 1f, Order = 2, RequireRestart = false,
            HintText = "{=ND_MCM_RELFACTOR_HINT}How strongly poor relations boost chance(or good decrease) (Default: 0.25).")]
        [SettingPropertyGroup("{=MCM_GENERAL}General")]
        public float RelationFactor { get; set; } = 0.25f;

        [SettingPropertyInteger(
            "{=ND_MCM_RELLEAVE}Relation Threshold to Leave",
            -100, 100, Order = 3, RequireRestart = false,
            HintText = "{=ND_MCM_RELLEAVE_HINT}Relation ≤ this makes nobles eligible. (Default: -10)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General")]
        public int RelationLeave { get; set; } = -10;

        [SettingPropertyInteger(
            "{=ND_MCM_RELLOSS}Relation Loss on Leave",
            -100, 100, Order = 4, RequireRestart = false,
            HintText = "{=ND_MCM_RELLOSS_HINT}Leader loses this when a noble departs. (Default: -20)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General")]
        public int RelationLoss { get; set; } = -20;

        [SettingPropertyInteger(
            "{=ND_MCM_RELGAIN}Relation Gain on Join",
            -100, 100, Order = 5, RequireRestart = false,
            HintText = "{=ND_MCM_RELGAIN_HINT}Leader gains this when a noble joins. (Default: 10)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General")]
        public int RelationGain { get; set; } = 10;

        [SettingPropertyInteger(
            "{=ND_MCM_CREATEWAY}Created Clan Type (Noble Or Mercenary)",
            0, 2, Order = 6, RequireRestart = false,
            HintText = "{=ND_MCM_CREATEWAY_HINT}0 - if clan leader is related to noble then clan is noble. Otherwise mercenary. 1 - only mercenary. 2 - only noble. (Default: 0)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General")]
        public int CreateWay { get; set; } = 0;


        //── Destination Weights ────────────────────────────────────────────

        [SettingPropertyFloatingInteger(
            "{=ND_MCM_WJOIN}Join Existing Clan Weight",
            0f, 1f, Order = 0, RequireRestart = false,
            HintText = "{=ND_MCM_WJOIN_HINT}Relative weight for joining. (Default: 0.5)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_DESTINATIONS}Destinations")]
        public float JoinExistingWeight { get; set; } = 0.5f;

        [SettingPropertyFloatingInteger(
            "{=ND_MCM_WNEW}Create New Clan Weight",
            0f, 1f, Order = 1, RequireRestart = false,
            HintText = "{=ND_MCM_WNEW_HINT}Relative weight for founding clan. (Default: 0.3)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_DESTINATIONS}Destinations")]
        public float CreateNewWeight { get; set; } = 0.3f;

        [SettingPropertyFloatingInteger(
            "{=ND_MCM_WWANDER}Become Wanderer Weight",
            0f, 1f, Order = 2, RequireRestart = false,
            HintText = "{=ND_MCM_WWANDER_HINT}Relative weight for wanderer. (Default: 0.2)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_DESTINATIONS}Destinations")]
        public float WandererWeight { get; set; } = 0.2f;

        //── Family Options ─────────────────────────────────────────────────

        [SettingPropertyBool(
            "{=ND_MCM_SPOUSE}Include Spouse",
            Order = 0, RequireRestart = false,
            HintText = "{=ND_MCM_SPOUSE_HINT}Spouse leaves/join with hero. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_FAMILY}Family")]
        public bool IncludeSpouse { get; set; } = true;

        [SettingPropertyBool(
            "{=ND_MCM_CHILDREN}Include Children",
            Order = 1, RequireRestart = false,
            HintText = "{=ND_MCM_CHILDREN_HINT}Children leave/join with hero. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_FAMILY}Family")]
        public bool IncludeChildren { get; set; } = true;

        [SettingPropertyInteger(
            "{=ND_MCM_FAMREL}Family Relation Threshold",
            0, 100, Order = 2, RequireRestart = false,
            HintText = "{=ND_MCM_FAMREL_HINT}Relation needed to take family. (Default: 5)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_FAMILY}Family")]
        public int FamilyRelation { get; set; } = 5;

        [SettingPropertyBool(
            "{=ND_MCM_DIVORCE}Enable Divorce",
            Order = 3, RequireRestart = false,
            HintText = "{=ND_MCM_DIVORCE_HINT}If hero leaves clan without spouse, they are divorcing. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_FAMILY}Family")]
        public bool EnableDivorce { get; set; } = true;

        [SettingPropertyButton("{=ND_MCM_DIVORCE_ALL}Divorce Wanderers", Content = "{=ND_MCM_DIVORCE_ALL}Divorce", Order = 4, RequireRestart = false, HintText = "{=ND_MCM_DIVORCE_HINT}All wanderers will be divorced. ")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_FAMILY}Family")]
        public Action DivorceWanderers { get; set; }

        //── Limits & Filters ───────────────────────────────────────────────

        [SettingPropertyInteger(
            "{=ND_MCM_MAXCLANS}Max Active Clans",
            1, 1000, Order = 0, RequireRestart = false,
            HintText = "{=ND_MCM_MAXCLANS_HINT}Above this number, nobles won't be creating clans. (Default: 100)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_LIMITS}Limits")]
        public int MaxActiveClans { get; set; } = 100;

        [SettingPropertyInteger(
            "{=ND_MCM_MAXWAND}Max Active Wanderers",
            1, 1000, Order = 1, RequireRestart = false,
            HintText = "{=ND_MCM_MAXWAND_HINT}Above this number, nobles won't become wanderers. (Default: 70)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_LIMITS}Limits")]
        public int MaxActiveWanderers { get; set; } = 70;

        [SettingPropertyBool(
            "{=ND_MCM_ALLOWWAND}Allow Wanderers to Depart",
            Order = 2, RequireRestart = false,
            HintText = "{=ND_MCM_ALLOWWAND_HINT}Wanderers in clans may depart. (Default: false)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_LIMITS}Limits")]
        public bool AllowWanderer { get; set; } = false;

        [SettingPropertyBool(
            "{=ND_MCM_ALLOWCROWN}Allow Crown Children to Depart",
            Order = 2, RequireRestart = false,
            HintText = "{=ND_MCM_ALLOWCROWN_HINT}Crown Children in clans may depart. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_LIMITS}Limits")]
        public bool AllowCrown { get; set; } = true;

        [SettingPropertyBool(
            "{=ND_MCM_ALLOWLEADCHILD}Allow Leader Clan Children to Depart",
            Order = 2, RequireRestart = false,
            HintText = "{=ND_MCM_ALLOWLEADCHILD_HINT}Leader Clan Children in clans may depart. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_LIMITS}Limits")]
        public bool AllowLeadChild { get; set; } = true;

        [SettingPropertyBool(
            "{=ND_MCM_ALLOWPLRDEP}Player Clan Departure",
            Order = 3, RequireRestart = false,
            HintText = "{=ND_MCM_ALLOWPLRDEP_HINT}Allow nobles leaving your clan. (Default: false)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_LIMITS}Limits")]
        public bool AllowPlayerClanMembersDeparture { get; set; } = false;

        [SettingPropertyBool(
            "{=ND_MCM_ALLOWJOINPLR}Nobles May Join Player Clan",
            Order = 4, RequireRestart = false,
            HintText = "{=ND_MCM_ALLOWJOINPLR_HINT}Allow petition to join your clan. (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_LIMITS}Limits")]
        public bool AllowJoinPlayerClan { get; set; } = true;


        //── Fiefs & Starting Resources ────────────────────────────────────

        [SettingPropertyInteger(
            "{=ND_MCM_FIEFS}Fiefs Amount to Transfer",
            0, 20, Order = 0, RequireRestart = false,
            HintText = "{=ND_MCM_FIEFS_HINT}How many fiefs move to new clan. (Default: 0)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_STARTUP}Startup")]
        public int FiefsAmount { get; set; } = 0;

        [SettingPropertyInteger(
            "{=ND_MCM_MINRENOWN}Min Renown for New Clan",
            0, 100000, Order = 1, RequireRestart = false,
            HintText = "{=ND_MCM_MINRENOWN_HINT}Minimum renown given. (Default: 50)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_STARTUP}Startup")]
        public int MinRenown { get; set; } = 50;

        [SettingPropertyInteger(
            "{=ND_MCM_MAXRENOWN}Max Renown for New Clan",
            0, 100000, Order = 2, RequireRestart = false,
            HintText = "{=ND_MCM_MAXRENOWN_HINT}Maximum renown given. (Default: 6250)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_STARTUP}Startup")]
        public int MaxRenown { get; set; } = 6250;

        [SettingPropertyInteger(
            "{=ND_MCM_MINGOLD}Min Gold for Founders",
            0, 1000000, Order = 3, RequireRestart = false,
            HintText = "{=ND_MCM_MINGOLD_HINT}Min starting gold. (Default: 50)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_STARTUP}Startup")]
        public int MinGold { get; set; } = 50;

        [SettingPropertyInteger(
            "{=ND_MCM_MAXGOLD}Max Gold for Founders",
            0, 1000000, Order = 4, RequireRestart = false,
            HintText = "{=ND_MCM_MAXGOLD_HINT}Max starting gold. (Default: 600000)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_STARTUP}Startup")]
        public int MaxGold { get; set; } = 600000;

        //── Debug & Notifications ─────────────────────────────────────────

        [SettingPropertyBool(
            "{=ND_MCM_DEBUG}Enable Debug Messages",
            Order = 0, RequireRestart = false,
            HintText = "{=ND_MCM_DEBUG_HINT}Shows debug to log. (Default: false)")]
        [SettingPropertyGroup("{=MCM_NOTIFY}Notify")]
        public bool Debug { get; set; } = false;

        [SettingPropertyBool(
            "{=ND_MCM_INFORM}Inform on NPC Clan Changes",
            Order = 1, RequireRestart = false,
            HintText = "{=ND_MCM_INFORM_HINT}Notify when non‑player nobles move. (Default: true)")]
        [SettingPropertyGroup("{=MCM_NOTIFY}Notify")]
        public bool Inform { get; set; } = true;

        [SettingPropertyBool(
            "{=ND_MCM_INFORMPLR}Inform on Player Clan Changes",
            Order = 2, RequireRestart = false,
            HintText = "{=ND_MCM_INFORMPLR_HINT}Notify when they leave/join your clan. (Default: true)")]
        [SettingPropertyGroup("{=MCM_NOTIFY}Notify")]
        public bool InformPlayer { get; set; } = true;

        [SettingPropertyBool(
            "{=ND_MCM_STATINFO}Show Settings on Load",
            Order = 3, RequireRestart = false,
            HintText = "{=ND_MCM_STATINFO_HINT}Display settings at game‑start. (Default: true)")]
        [SettingPropertyGroup("{=MCM_NOTIFY}Notify")]
        public bool StatInfo { get; set; } = true;

        //── Wanderer‑Clan Creation ────────────────────────────────────────

        [SettingPropertyBool(
            "{=ND_MCM_WANDCREATE}Allow Wanderers Create Clans",
            Order = 0, RequireRestart = false,
            HintText = "{=ND_MCM_WANDCREATE_HINT}Clanless wanderers may found clans. (Default: true)")]
        [SettingPropertyGroup("{=MCM_WANDERERS_CLAN_CREATION}Wanderers Clan Creation")]
        public bool WandererCreate { get; set; } = true;

        [SettingPropertyInteger(
            "{=ND_MCM_WANDNUM}Wanderers Needed to Create",
            1, 20, Order = 1, RequireRestart = false,
            HintText = "{=ND_MCM_WANDNUM_HINT}Number of wanderers to form a clan. (Default: 3)")]
        [SettingPropertyGroup("{=MCM_WANDERERS_CLAN_CREATION}Wanderers Clan Creation")]
        public int WandererCreateNumber { get; set; } = 3;

        [SettingPropertyFloatingInteger(
            "{=ND_MCM_WANDPROB}Wanderer Create Probability",
            0f, 1f, "#0%",
            Order = 2, RequireRestart = false,
            HintText = "{=ND_MCM_WANDPROB_HINT}Daily chance wanderers form a clan. (Default: 0.15)")]
        [SettingPropertyGroup("{=MCM_WANDERERS_CLAN_CREATION}Wanderers Clan Creation")]
        public float WandererCreateProbability { get; set; } = 0.15f;

        [SettingPropertyInteger(
            "{=ND_MCM_WANDWAY}Created Clan Type (Noble Or Mercenary)",
            0, 2, Order = 2, RequireRestart = false,
            HintText = "{=ND_MCM_WANDWAY_HINT}0 - if clan leader is related to noble then clan is noble. Otherwise mercenary. 1 - only mercenary. 2 - only noble. (Default: 0)")]
        [SettingPropertyGroup("{=MCM_WANDERERS_CLAN_CREATION}Wanderers Clan Creation")]
        public int WandererWay { get; set; } = 0;

        [SettingPropertyBool(
            "{=ND_MCM_WANDNOBLE}Noble Kit",
            Order = 3, RequireRestart = false,
            HintText = "{=ND_MCM_WANDNOBLE_HINT}Should wanderers get noble equipment? (Default: true)")]
        [SettingPropertyGroup("{=MCM_WANDERERS_CLAN_CREATION}Wanderers Clan Creation")]
        public bool WandererNoble { get; set; } = true;

        //--- Names ------------

        [SettingPropertyBool(
            "{=ND_MCM_NOBLE_NAME_CHANGE}Append 'The Wanderer'?",
            Order = 0, RequireRestart = false,
            HintText = "{=ND_MCM_NOBLE_NAME_CHANGE_HINT}Should nobles who become wanderers have 'The Wanderer' appended? (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_NAMES}Names")]
        public bool NobleNameChange { get; set; } = true;

        [SettingPropertyBool(
            "{=ND_MCM_WAND_NAME_CHANGE}Remove suffix?",
            Order = 1, RequireRestart = false,
            HintText = "{=ND_MCM_WAND_NAME_CHANGE_HINT}Should wanderers who become nobles have suffix like 'The Wanderer' removed? (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_NAMES}Names")]
        public bool WandNameChange { get; set; } = true;

        //── Kingdom Formation ─────────────────────────────────────────────

        [SettingPropertyFloatingInteger(
            "{=ND_MCM_CREATEK}Kingdom Creation Probability",
            0f, 1f, "#0%",
            Order = 0, RequireRestart = false,
            HintText = "{=ND_MCM_CREATEK_HINT}Chance new clan founds a kingdom. (Default: 0.15)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_KINGDOM}Kingdom")]
        public float CreateKingdom { get; set; } = 0.15f;

        [SettingPropertyFloatingInteger(
            "{=ND_MCM_CULTJOINK}Culture Kingdom Affinity Bonus",
            -100f, 100f, Order = 2, RequireRestart = false,
            HintText = "{=ND_MCM_CULTJOINK_HINT}How much culture play role in choosing kingdom to join. (relation + amountFiefs + cultureBonus) (Default: 10)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_KINGDOM}Kingdom")]
        public float CultureJoinKingdom { get; set; } = 10f;

        //── Clan Name Suffixes ────────────────────────────────────────────

        [SettingPropertyText(
            "{=ND_MCM_CLANSUFF}Clan Name Suffixes",
            Order = 0, RequireRestart = false,
            HintText = "{=ND_MCM_CLANSUFF_HINT}Comma‑separate suffixes.")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_NAMES}Names/{=MCM_CLANS}Clans")]
        public string ClanNameSuffixesCsv { get; set; } =
            "Clan,House,Dynasty,Line,Order,Union,Purge";

        //── Kingdom Name Suffixes & Titles ───────────────────────────────

        [SettingPropertyText(
            "{=ND_MCM_KINGSUFF}Kingdom Name Suffixes",
            Order = 0, RequireRestart = false,
            HintText = "{=ND_MCM_KINGSUFF_HINT}(kingdom: title, etc). Title can be random because game takes it from culture.")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_NAMES}Names/{=MCM_KINGDOMS}Kingdoms")]
        public string KingdomNameSuffixesJson { get; set; } =
            "Kingdom:King, Empire:Emperor, Sultanate:Sultan, Dominion:King, State:President, Union:King, Republic:Chancellor";

        //────Cultures + Traits──────────────────────────────────────────────

        [SettingPropertyText(
            "{=ND_MCM_CULT_LEAVE}Leave‑Chance Bonus",
            Order = 0, RequireRestart = false,
            HintText = "{=ND_MCM_CULT_LEAVE_HINT}(culture ID : leave bonus, etc)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_CULTURE_MODIFIERS}Culture Modifiers")]
        public string CultureLeaveChanceJson { get; set; } =
            "empire: 0.1, aserai: -0.1, sturgia: 0.0, battania: -0.1, khuzait: 0.1 ";

        [SettingPropertyText(
            "{=ND_MCM_CULT_JOIN}Join‑Chance Bonus",
            Order = 1, RequireRestart = false,
            HintText = "{=ND_MCM_CULT_JOIN_HINT}(culture ID : join bonus, etc)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_CULTURE_MODIFIERS}Culture Modifiers")]
        public string CultureJoinChanceJson { get; set; } =
            "empire: 0.1, aserai: -0.1, sturgia: -0.1, battania: 0.1, khuzait: 0.0";

        [SettingPropertyText(
            "{=ND_MCM_CULT_CREATE}Create‑Clan Bonus",
            Order = 2, RequireRestart = false,
            HintText = "{=ND_MCM_CULT_CREATE_HINT}(culture ID : create bonus, etc)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_CULTURE_MODIFIERS}Culture Modifiers")]
        public string CultureCreateChanceJson { get; set; } =
            "empire: 0.0, aserai: 0.1, sturgia: 0.0, battania: -0.1, khuzait: -0.1 ";

        [SettingPropertyText(
            "{=ND_MCM_CULT_WANDER}Wanderer Bonus",
            Order = 3, RequireRestart = false,
            HintText = "{=ND_MCM_CULT_WANDER_HINT}(culture ID : wander bonus, etc)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_CULTURE_MODIFIERS}Culture Modifiers")]
        public string CultureWanderChanceJson { get; set; } =
            "empire: -0.1, aserai: -0.1, sturgia: 0.0, battania: 0.0, khuzait: 0.1";

        [SettingPropertyText(
            "{=ND_MCM_TRAIT}Trait Influence Bonus",
            Order = 4, RequireRestart = false,
            HintText = "{=ND_MCM_TRAIT_HINT}(trait id : multiplier).")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_TRAIT_MODIFIERS}Trait Modifiers")]
        public string TraitModifiersJson { get; set; } =
            "Mercy: -0.02, Honor: -0.02, Generosity: 0.00, Valor: 0.02, Calculating: 0.02";

        //--- Dialogues ------------

        [SettingPropertyBool(
            "{=ND_MCM_KICKDIAL}Enable 'Kick From The Clan' Dialogue",
            Order = 0, RequireRestart = false,
            HintText = "{=ND_MCM_KICKDIAL_HINT}Enable/Disable 'Kick From The Clan' dialogue (needs save reload) (Default: true)")]
        [SettingPropertyGroup("{=MCM_GENERAL}General/{=MCM_DIALOGUES}Dialogues")]
        public bool KickDial { get; set; } = true;

    }

}
