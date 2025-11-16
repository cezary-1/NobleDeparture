namespace DynamicNobleDeparture
{
    public static class DynamicNobleDepartureSettings
    {
        // General
        public static float LeavingChance { get; set; } = 0.01f;
        public static float RelationWeight { get; set; } = 1f;

        // Destinations
        public static float JoinWeight { get; set; } = 1f;
        public static float NewClanWeight { get; set; } = 1f;
        public static float WandererWeight { get; set; } = 1f;

        // Limits
        public static int MaxActiveClans { get; set; } = 50;

        // Family
        public static bool IncludeSpouse { get; set; } = true;
        public static bool IncludeChildren { get; set; } = false;

        // Messages
        public static bool ShowPlayerMessages { get; set; } = true;
        public static bool ShowOtherMessages { get; set; } = false;

        // Dummy instance for easy reference
        public static DynamicNobleDepartureSettings Instance => new DynamicNobleDepartureSettings();
    }

    // An empty class matching the `Instance` return type
    public class DynamicNobleDepartureSettings { }
}
