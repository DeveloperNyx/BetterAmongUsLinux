using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using BetterAmongUs.Modules.OptionItems;
using BetterAmongUs.Modules.Support;
using HarmonyLib;
using UnityEngine;

namespace BetterAmongUs.Patches.Gameplay;

[HarmonyPatch]
internal static class LobbyPatch
{
    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
    [HarmonyPostfix]
    private static void LobbyBehaviour_Start_Postfix()
    {
        OptionPlayerItem.ResetAllValues();
    }

    // Disabled annoying music
    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Update))]
    [HarmonyPostfix]
    private static void LobbyBehaviour_Update_Postfix()
    {
        if (BAUPlugin.DisableLobbyTheme.Value)
            SoundManager.instance.StopSound(LobbyBehaviour.Instance.MapTheme);
    }

    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.RpcExtendLobbyTimer))]
    [HarmonyPostfix]
    private static void LobbyBehaviour_RpcExtendLobbyTimer_Postfix()
    {
        lobbyTimer += 30f;
    }

    [HarmonyPatch(typeof(LobbyViewSettingsPane), nameof(LobbyViewSettingsPane.Awake))]
    [HarmonyPostfix]
    private static void LobbyViewSettingsPane_Awake_Postfix(LobbyViewSettingsPane __instance)
    {
        __instance.backButton.gameObject.SetUIColors("Icon");
        __instance.taskTabButton.gameObject.SetUIColors("Icon");
        __instance.rolesTabButton.gameObject.SetUIColors("Icon");
    }

    internal static float lobbyTimer = 600f;
    internal static string lobbyTimerDisplay = "";

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    [HarmonyPostfix]
    private static void GameStartManager_Start_Postfix(GameStartManager __instance)
    {
        lobbyTimer = 600f;

        __instance.StartButton?.gameObject?.SetUIColors("Icon");
        __instance.EditButton?.gameObject?.SetUIColors("Icon");
        __instance.ClientViewButton?.gameObject?.SetUIColors("Icon");
        __instance.HostViewButton?.gameObject?.SetUIColors("Icon");

        if (!BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_BetterPingTracker))
        {
            __instance.StartButton?.transform?.SetParent(__instance.HostInfoPanel?.transform);
            __instance.StartButtonClient?.transform?.SetParent(__instance.HostInfoPanel?.transform);
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    [HarmonyPrefix]
    private static void GameStartManager_Update_Prefix(GameStartManager __instance)
    {
        lobbyTimer = Mathf.Max(0f, lobbyTimer -= Time.deltaTime);
        int minutes = (int)lobbyTimer / 60;
        int seconds = (int)lobbyTimer % 60;
        lobbyTimerDisplay = $"{minutes:00}:{seconds:00}";

        __instance.MinPlayers = 1;
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    [HarmonyPostfix]
    private static void GameStartManager_Update_Postfix(GameStartManager __instance)
    {
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_CancelStartingGame)) return;

        if (!GameState.IsHost)
        {
            __instance.StartButton.gameObject.SetActive(false);
            return;

        }
        __instance.GameStartTextParent.SetActive(false);
        __instance.StartButton.gameObject.SetActive(true);
        if (__instance.startState == GameStartManager.StartingStates.Countdown)
        {
            __instance.StartButton.buttonText.text = string.Format("{0}: {1}", Translator.GetString(StringNames.Cancel), (int)__instance.countDownTimer + 1);
        }
        else
        {
            __instance.StartButton.buttonText.text = Translator.GetString(StringNames.StartLabel);
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
    [HarmonyPrefix]
    private static bool GameStartManager_BeginGame_Prefix(GameStartManager __instance)
    {
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_CancelStartingGame)) return true;

        if (__instance.startState == GameStartManager.StartingStates.Countdown)
        {
            SoundManager.instance.StopSound(__instance.gameStartSound);
            __instance.ResetStartState();
            return false;
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            __instance.startState = GameStartManager.StartingStates.Countdown;
            __instance.FinallyBegin();
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.FinallyBegin))]
    [HarmonyPrefix]
    private static void GameStartManager_FinallyBegin_Prefix(/*GameStartManager __instance*/)
    {
        Logger_.LogHeader($"Game Has Started - {Enum.GetName(typeof(MapNames), GameState.GetActiveMapId)}/{GameState.GetActiveMapId}", "GamePlayManager");
    }
}
