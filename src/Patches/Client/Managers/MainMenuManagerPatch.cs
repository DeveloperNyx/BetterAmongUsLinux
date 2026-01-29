using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Modules.Support;
using HarmonyLib;
using UnityEngine;

namespace BetterAmongUs.Patches.Client.Managers;

[HarmonyPatch]
internal static class MainMenuManagerPatch
{
    internal static PassiveButton? ButtonPrefab;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate))]
    [HarmonyPostfix]
    private static void MainMenuManager_LateUpdate_Postfix(MainMenuManager __instance)
    {
        List<PassiveButton> buttons = [__instance.playButton, __instance.inventoryButton, __instance.shopButton, __instance.playLocalButton, __instance.PlayOnlineButton, __instance.backButtonOnline,
            __instance.newsButton, __instance.myAccountButton, __instance.settingsButton, __instance.howToPlayButton, __instance.freePlayButton, __instance.accountCTAButton, __instance.accountStatsButton];
        foreach (var button in buttons)
        {
            button.gameObject?.SetUIColors(sprite =>
            {
                return sprite.color == Color.white;
            },
            "Icon", "Background");
        }
    }

    // Replace AU logo with BAU logo
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    [HarmonyPostfix]
    private static void MainMenuManager_Start_Postfix(MainMenuManager __instance)
    {
        if (!BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_BAULogo))
        {
            GameObject logo = GameObject.Find("LeftPanel/Sizer/LOGO-AU");
            GameObject sizer = logo.transform.parent.gameObject;
            sizer.transform.localPosition = new Vector3(sizer.transform.localPosition.x, sizer.transform.localPosition.y - 0.035f, sizer.transform.localPosition.z);
            sizer.transform.position = new Vector3(sizer.transform.position.x, sizer.transform.position.y, -0.5f);
            logo.transform.localScale = new Vector3(0.003f, 0.0025f, 0f);
            logo.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("BetterAmongUs.Resources.Images.BetterAmongUs-Logo.png", 1f);
        }

        __instance.transform.Find("MainUI/AspectScaler/BackgroundTexture")?.gameObject?.SetSpriteColors(sprite => ObjectHelper.AddColor(sprite));

        if (ButtonPrefab == null)
        {
            ButtonPrefab = UnityEngine.Object.Instantiate(__instance.inventoryButton);
            ButtonPrefab.gameObject.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(ButtonPrefab);
        }

        UpdateManager.Instance?.OnMainMenu();
    }
}
