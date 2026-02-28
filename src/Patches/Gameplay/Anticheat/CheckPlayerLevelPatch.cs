using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using BetterAmongUs.Patches.Gameplay.UI.Settings;
using HarmonyLib;

namespace BetterAmongUs.Patches.Gameplay.Anticheat;

[HarmonyPatch]
internal class CheckPlayerLevelPatch
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    [HarmonyPostfix]
    private static void PlayerControl_FixedUpdate_Postfix(PlayerControl __instance)
    {
        if (GameState.IsHost)
        {
            // Kick players below minimum level
            if (!__instance.IsLocalPlayer() && __instance.Data.PlayerLevel < BetterGameSettings.KickLevelBelow.GetInt())
            {
                __instance.Kick(setReasonInfo: $" is level {__instance.Data.PlayerLevel}, level must be equal or above {BetterGameSettings.KickLevelBelow.GetInt()} to join");
            }
        }
    }
}