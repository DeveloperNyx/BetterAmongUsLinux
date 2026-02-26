using AmongUs.GameOptions;
using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using HarmonyLib;
using UnityEngine;

namespace BetterAmongUs.Patches.Gameplay;

[HarmonyPatch]
internal class ZoomPatch
{
    private static bool _wasZooming = false;
    private static float _lastOrthographicSize = 0f;

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    private static void HudManager_Update_Postfix()
    {
        bool canZoom = GameState.IsCanMove &&
              !PlayerControl.LocalPlayer.Is(RoleTypes.GuardianAngel) &&
              (!GameState.IsInGamePlay || !PlayerControl.LocalPlayer.IsAlive());

        bool shouldReset = !canZoom && _wasZooming;

        _lastOrthographicSize = Camera.main.orthographicSize;

        if (shouldReset)
        {
            SetZoomSize(reset: true);
            _wasZooming = false;
        }
        else if (canZoom)
        {
            if (Input.mouseScrollDelta.y > 0 && Camera.main.orthographicSize > 3.0f)
            {
                _wasZooming = true;
                SetZoomSize(zoomIn: true);
            }
            else if (Input.mouseScrollDelta.y < 0 &&
                    (GameState.IsDead || GameState.IsFreePlay || GameState.IsLobby) &&
                    Camera.main.orthographicSize < 18.0f)
            {
                _wasZooming = true;
                SetZoomSize(zoomOut: true);
            }
        }
    }

    private static void SetZoomSize(bool zoomIn = false, bool zoomOut = false, bool reset = false)
    {
        if (reset)
        {
            Camera.main.orthographicSize = 3.0f;
            HudManager.Instance.UICamera.orthographicSize = 3.0f;
            HudManager.Instance.Chat.transform.localScale = Vector3.one;

            if (GameState.IsMeeting)
                MeetingHud.Instance.transform.localScale = Vector3.one;
        }
        else if (zoomIn || zoomOut)
        {
            float size = zoomIn ? 1 / 1.5f : 1.5f;
            Camera.main.orthographicSize *= size;
            HudManager.Instance.UICamera.orthographicSize *= size;
        }

        HudManager.Instance?.ShadowQuad?.gameObject?.SetActive(
            (reset || Camera.main.orthographicSize == 3.0f) &&
            PlayerControl.LocalPlayer.IsAlive());

        if (Camera.main.orthographicSize != _lastOrthographicSize)
        {
            ResolutionManager.ResolutionChanged.Invoke(
                (float)Screen.width / Screen.height,
                Screen.width, Screen.height,
                Screen.fullScreen);
        }
    }
}
