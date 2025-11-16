using HarmonyLib;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace NobleDeparture
{
    //template fix

    [HarmonyPatch(typeof(Hero), nameof(Hero.Template), MethodType.Getter)]
    static class GetHeroTemplate
    {

        [HarmonyPostfix]
        static void HeroTemplate_Postfix(ref CharacterObject __result, Hero __instance)
        {
            if (__result != null) return; // run original
            __result = __instance.CharacterObject ?? CharacterObject.All.FirstOrDefault(c => c.IsTemplate && c.Occupation == Occupation.Wanderer);               // return wanderer
        }
    }
}
