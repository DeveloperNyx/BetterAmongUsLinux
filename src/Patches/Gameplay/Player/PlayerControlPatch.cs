using BepInEx.Unity.IL2CPP.Utils.Collections;
using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using BetterAmongUs.Modules.OptionItems;
using BetterAmongUs.Mono;
using HarmonyLib;
using System.Collections;

namespace BetterAmongUs.Patches.Gameplay.Player;

[HarmonyPatch]
internal static class PlayerControlPatch
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    [HarmonyPostfix]
    private static void PlayerControl_Start_Postfix(PlayerControl __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        // Add player to global player list
        BAUPlugin.AllPlayerControls.Add(__instance);

        // Update option UI values for all players
        OptionPlayerItem.UpdateAllValues();

        // Append favorite color setting to player initialization coroutine
        __result = Effects.Sequence(__result, CoSetFavoriteColor(__instance).WrapToIl2Cpp());
    }

    private static IEnumerator CoSetFavoriteColor(PlayerControl player)
    {
        if (!GameState.IsLobby) yield break;

        // Apply player's favorite color setting if they own this character
        if (player.AmOwner)
        {
            if (BAUPlugin.FavoriteColor.Value >= 0 && player.cosmetics.ColorId != (byte)BAUPlugin.FavoriteColor.Value)
            {
                // Send command to server to change color
                player.CmdCheckColor((byte)BAUPlugin.FavoriteColor.Value);
            }
        }

        yield break;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.OnDestroy))]
    [HarmonyPostfix]
    private static void PlayerControl_OnDestroy_Postfix(PlayerControl __instance)
    {
        // Remove player from global list when destroyed
        BAUPlugin.AllPlayerControls.Remove(__instance);

        // Update option UI values
        OptionPlayerItem.UpdateAllValues();
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    [HarmonyPostfix]
    private static void PlayerControl_MurderPlayer_Postfix(PlayerControl __instance, PlayerControl target)
    {
        if (target == null) return;

        // Log kill event with player names and roles
        Logger_.LogPrivate($"{__instance.Data.PlayerName} Has killed {target.Data.PlayerName} as {__instance.Data.RoleType.GetRoleName()}", "EventLog");

        // Track kill count in player's BetterData
        __instance.BetterData().RoleInfo.Kills += 1;
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
    [HarmonyPostfix]
    private static void PlayerControl_Shapeshift_Postfix(PlayerControl __instance, PlayerControl targetPlayer, bool animate)
    {
        if (targetPlayer == null) return;

        // Log shapeshift events (both shifting and unshifting)
        if (__instance != targetPlayer)
            Logger_.LogPrivate($"{__instance.Data.PlayerName} Has Shapeshifted into {targetPlayer.Data.PlayerName}, did animate: {animate}", "EventLog");
        else
            Logger_.LogPrivate($"{__instance.Data.PlayerName} Has Un-Shapeshifted, did animate: {animate}", "EventLog");
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetRoleInvisibility))]
    [HarmonyPostfix]
    private static void PlayerControl_SetRoleInvisibility_Postfix(PlayerControl __instance, bool isActive, bool shouldAnimate)
    {
        // Log Phantom role visibility changes
        if (isActive)
            Logger_.LogPrivate($"{__instance.Data.PlayerName} Has Vanished as Phantom, did animate: {shouldAnimate}", "EventLog");
        else
            Logger_.LogPrivate($"{__instance.Data.PlayerName} Has Appeared as Phantom, did animate: {shouldAnimate}", "EventLog");
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetName))]
    [HarmonyPostfix]
    private static void PlayerControl_SetName_Postfix(PlayerControl __instance, string playerName)
    {
        // Store the last set name in player's BetterData
        __instance.BetterDataWait(data =>
        {
            data.NameSetAsLast = playerName;
        });
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.BootFromVent))]
    [HarmonyPostfix]
    private static void PlayerPhysics_BootFromVent_Postfix(PlayerPhysics __instance, int ventId)
    {
        // Log vent boot events (when engineer boots someone)
        Logger_.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has been booted from vent: {ventId}, as {__instance.myPlayer.Data.RoleType.GetRoleName()}", "EventLog");
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    [HarmonyPostfix]
    private static void PlayerPhysics_CoEnterVent_Postfix(PlayerPhysics __instance, int id)
    {
        // Log vent entry events
        Logger_.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has entered vent: {id}, as {__instance.myPlayer.Data.RoleType.GetRoleName()}", "EventLog");
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoExitVent))]
    [HarmonyPostfix]
    private static void PlayerPhysics_CoExitVent_Postfix(PlayerPhysics __instance, int id)
    {
        // Log vent exit events
        Logger_.LogPrivate($"{__instance.myPlayer.Data.PlayerName} Has exit vent: {id}, as {__instance.myPlayer.Data.RoleType.GetRoleName()}", "EventLog");
    }
}