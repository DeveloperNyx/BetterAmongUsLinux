using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using BetterAmongUs.Modules.Support;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BetterAmongUs.Patches.Client;

[HarmonyPatch]
internal static class PrivateLobbyPatch
{
    private static GameObject? toggle;
    private static List<PassiveButton>? buttons = [];
    private static TextMeshPro? toggleText;

    [HarmonyPatch(typeof(CreateGameOptions), nameof(CreateGameOptions.Show))]
    [HarmonyPostfix]
    private static void CreateGameOptions_Show_Postfix(CreateGameOptions __instance)
    {
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_PrivateLobby))
        {
            BAUPlugin.PrivateOnlyLobby.Value = false;
            return;
        }

        if (toggle != null) return;
        buttons.Clear();

        toggle = UnityEngine.Object.Instantiate(__instance.AprilFoolsToggle, __instance.contentObjects.First().transform.parent);
        if (toggle != null)
        {
            toggle.name = "PrivateOnlyLobby";
            buttons.Add(toggle.transform.Find("AprilOn").GetComponent<PassiveButton>());
            buttons.Add(toggle.transform.Find("AprilOff").GetComponent<PassiveButton>());
            if (buttons.Count < 2) return;

            toggle.gameObject.SetActive(true);
            var onButton = buttons[0];
            if (onButton != null)
            {
                onButton.gameObject.SetActive(false);
                onButton.OnClick = new();
                onButton.OnClick.AddListener((Action)(() => TogglePrivateOnlyLobby(true)));
            }
            var offButton = buttons[1];
            if (offButton != null)
            {
                offButton.gameObject.SetActive(false);
                offButton.OnClick = new();
                offButton.OnClick.AddListener((Action)(() => TogglePrivateOnlyLobby(false)));
            }
            var aspect = toggle.gameObject.AddComponent<AspectPosition>();
            aspect.Alignment = AspectPosition.EdgeAlignments.Top;
            aspect.DistanceFromEdge = new Vector3(0.4f, 1.62f, 0);
            aspect.AdjustPosition();

            var text = toggle.transform.Find("BlackSquare/ModeText")?.GetComponent<TextMeshPro>();
            if (text != null)
            {
                text.DestroyTextTranslators();
                text.text = "Private Only Lobby";
                toggleText = text;
            }
        }

        TogglePrivateOnlyLobby(BAUPlugin.PrivateOnlyLobby.Value);
    }

    private static void TogglePrivateOnlyLobby(bool modeOn)
    {
        if (buttons.Count < 2) return;

        buttons[0].gameObject.SetActive(true);
        buttons[1].gameObject.SetActive(true);
        buttons[0].SelectButton(false);
        buttons[1].SelectButton(false);

        if (modeOn)
        {
            buttons[0].SelectButton(true);
        }
        else
        {
            buttons[1].SelectButton(true);
        }
        BAUPlugin.PrivateOnlyLobby.Value = modeOn;
    }

    [HarmonyPatch(typeof(LobbyInfoPane), nameof(LobbyInfoPane.Update))]
    [HarmonyPostfix]
    private static void LobbyInfoPane_Update_Postfix(LobbyInfoPane __instance)
    {
        if (BAUPlugin.PrivateOnlyLobby.Value && !GameState.IsLocalGame && GameState.IsHost)
        {
            if (AmongUsClient.Instance.IsGamePublic)
            {
                AmongUsClient.Instance.ChangeGamePublic(false);
            }

            var button = __instance.HostPrivateButton.GetComponent<PassiveButton>();
            if (button != null)
            {
                button.enabled = false;

                var sprite = __instance.HostPrivateButton.transform.Find("Inactive").GetComponent<SpriteRenderer>();
                if (sprite != null)
                {
                    sprite.color = new(0.35f, 1, 1, 1);
                }
            }
        }
    }
}
