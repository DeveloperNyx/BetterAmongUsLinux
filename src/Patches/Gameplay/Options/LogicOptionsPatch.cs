using AmongUs.GameOptions;
using BetterAmongUs.Helpers;
using HarmonyLib;

namespace BetterAmongUs.Patches.Gameplay.Options;

[HarmonyPatch]
internal static class LogicOptionsPatch
{
    [HarmonyPatch(typeof(LogicOptionsNormal), nameof(LogicOptionsNormal.GetAnonymousVotes))]
    [HarmonyPostfix]
    private static void LogicOptionsNormal_Update_Postfix(MeetingHud __instance, ref bool __result)
    {
        if (PlayerControl.LocalPlayer == null)
            return;

        // Show anonymous votes when dead and not Guardian Angel
        if (!PlayerControl.LocalPlayer.IsAlive() && !PlayerControl.LocalPlayer.Is(RoleTypes.GuardianAngel))
        {
            __result = false;
        }
    }
}
