using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using BetterAmongUs.Mono;
using BetterAmongUs.Patches.Gameplay.UI.Chat;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BetterAmongUs.Patches.Gameplay.UI;

[HarmonyPatch]
internal static class MeetingHudPatch
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    private static void MeetingHud_Start_Postfix(MeetingHud __instance)
    {
        if (__instance == null || __instance.playerStates == null) return;

        foreach (var pva in __instance.playerStates)
        {
            if (pva == null) continue;

            var target = Utils.PlayerFromPlayerId(pva.TargetPlayerId);
            if (target == null) continue;

            pva.gameObject.AddComponent<MeetingInfoDisplay>().Init(target, pva);
        }

        if (!GameState.IsOnlineGame) return;

        // Add host icon to meeting hud
        if (__instance.ProceedButton != null && __instance.HostIcon != null)
        {
            __instance.ProceedButton.gameObject.transform.localPosition = new(-2.5f, 2.2f, 0);
            __instance.ProceedButton.gameObject.GetComponent<SpriteRenderer>().enabled = false;
            __instance.ProceedButton.GetComponent<PassiveButton>().enabled = false;
            __instance.HostIcon.enabled = true;
            __instance.HostIcon.gameObject.SetActive(true);
            __instance.ProceedButton.gameObject.SetActive(true);
            UpdateHostIcon();
            MeetingHud.Instance.ProceedButton.DestroyTextTranslators();
        }

        Logger_.LogHeader("Meeting Has Started");
    }

    internal static void UpdateHostIcon()
    {
        if (MeetingHud.Instance == null) return;
        if (GameData.Instance == null) return;

        var host = GameData.Instance.GetHost();
        if (host == null || host.BetterData() == null) return;

        var hostColor = host.Color;
        var hostRealName = host.BetterData().RealName;

        if (MeetingHud.Instance.HostIcon == null || MeetingHud.Instance.ProceedButton == null) return;

        PlayerMaterial.SetColors(hostColor, MeetingHud.Instance.HostIcon);
        MeetingHud.Instance.ProceedButton.gameObject.GetComponentInChildren<TextMeshPro>().text = string.Format(Translator.GetString("HostInMeeting"), hostRealName);
    }

    internal static float timeOpen = 0f;

    // Set player meeting info
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    [HarmonyPostfix]
    private static void MeetingHud_Update_Postfix(MeetingHud __instance)
    {
        timeOpen += Time.deltaTime;
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    [HarmonyPostfix]
    private static void MeetingHud_Close_Postfix()
    {
        timeOpen = 0f;
        Logger_.LogHeader("Meeting Has Ended");

        if (BAUPlugin.ChatInGameplay.Value && !GameState.IsFreePlay && PlayerControl.LocalPlayer.IsAlive())
        {
            ChatPatch.ClearPlayerChats();
        }
    }
}