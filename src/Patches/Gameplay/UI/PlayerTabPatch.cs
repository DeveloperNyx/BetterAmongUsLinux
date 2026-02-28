using BetterAmongUs.Data;
using BetterAmongUs.Data.Json;
using BetterAmongUs.Helpers;
using BetterAmongUs.Patches.Client.Managers;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BetterAmongUs.Patches.Gameplay.UI;

[HarmonyPatch]
internal static class PlayerTabPatch
{
    private static List<PassiveButton> presetButtons = []; // Outfit preset buttons
    private static float cooldown = 0f; // Button click cooldown

    [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.OnEnable))]
    [HarmonyPrefix]
    private static void PlayerTab_OnEnable_Prefix(PlayerTab __instance)
    {
        // Clean up old preset buttons
        foreach (var button in presetButtons.ToArray())
        {
            if (button == null) continue;
            UnityEngine.Object.Destroy(button.gameObject);
        }
        presetButtons.Clear();

        // Create preset buttons (0 = Among Us preset, 1-5 = custom presets)
        for (int i = 0; i <= 5; i++)
        {
            int currentI = i;
            var name = currentI == 0 ? "Among Us Preset" : $"Preset {i}";
            var button = __instance.CreateButton(name, new Vector3(2.5f, 1.55f - currentI * 0.45f, 0f), () =>
            {
                // Ignore if cooldown active or same preset selected
                if (cooldown > 0f || BetterDataManager.BetterDataFile.SelectedOutfitPreset == currentI) return;
                cooldown = 0.5f;

                // Update selected preset
                BetterDataManager.BetterDataFile.SelectedOutfitPreset = currentI;

                // Reset all button hover states
                foreach (var button in presetButtons)
                {
                    if (button == null) continue;
                    button.SetPassiveButtonHoverStateInactive();
                }

                // Load and apply outfit from preset
                var data = OutfitData.GetOutfitData(currentI);
                data.Load(() =>
                {
                    if (LoadPlayerOutfit(data))
                    {
                        __instance.PlayerPreview.UpdateFromLocalPlayer(PlayerMaterial.MaskType.None);
                    }
                    else
                    {
                        __instance.PlayerPreview.UpdateFromDataManager(PlayerMaterial.MaskType.None);
                    }
                });
            });
            presetButtons.Add(button);
        }
    }

    private static readonly List<SpriteRenderer> _favoriteIcons = [];

    [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.OnEnable))]
    [HarmonyPostfix]
    private static void PlayerTab_OnEnable_Postfix(PlayerTab __instance)
    {
        _favoriteIcons.Clear();

        // Add favorite functionality to color chips
        for (int i = 0; i < __instance.ColorChips.Count; i++)
        {
            var index = i;
            var colorChip = __instance.ColorChips[i];

            // Override click behavior
            colorChip.Button.OnClick = new();
            colorChip.Button.OnClick.AddListener((Action)(() =>
            {
                // Shift+Click = toggle favorite
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (BAUPlugin.FavoriteColor.Value == index)
                    {
                        BAUPlugin.FavoriteColor.Value = -1; // Remove favorite
                    }
                    else
                    {
                        BAUPlugin.FavoriteColor.Value = index; // Set favorite
                    }

                    UpdateFavorite();
                    return;
                }

                // Normal click = equip color
                __instance.ClickEquip();
            }));

            // Add favorite star indicator
            var checkBox = colorChip.PlayerEquippedForeground.transform.Find("CheckMark").GetComponentInChildren<SpriteRenderer>();
            var favoriteIcon = UnityEngine.Object.Instantiate(checkBox, colorChip.transform);
            favoriteIcon.color = Color.yellow;
            favoriteIcon.transform.localPosition -= new Vector3(0f, 0f, 15f);
            _favoriteIcons.Add(favoriteIcon);
        }

        UpdateFavorite();
    }

    // Update favorite star visibility
    private static void UpdateFavorite()
    {
        for (int i = 0; i < _favoriteIcons.Count; i++)
        {
            SpriteRenderer? fav = _favoriteIcons[i];
            fav.gameObject.SetActive(i == BAUPlugin.FavoriteColor.Value);
        }
    }

    // Apply outfit to local player
    private static bool LoadPlayerOutfit(OutfitData data)
    {
        var player = PlayerControl.LocalPlayer;
        if (player != null)
        {
            player.RpcSetHat(data.HatId);
            player.RpcSetPet(data.PetId);
            player.RpcSetSkin(data.SkinId);
            player.RpcSetVisor(data.VisorId);
            player.RpcSetNamePlate(data.NamePlateId);
            return true;
        }

        return false;
    }

    // Helper to create preset buttons
    private static PassiveButton CreateButton(this PlayerTab __instance, string name, Vector3 pos, Action callback)
    {
        var button = UnityEngine.Object.Instantiate(MainMenuManagerPatch.ButtonPrefab, __instance.transform);
        button.gameObject.SetActive(true);
        button.gameObject.SetLayers("UI");
        button.transform.localPosition = pos;
        button.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        button.OnClick = new();
        button.OnClick.AddListener(callback);
        button.DestroyTextTranslators();
        var text = button.GetComponentInChildren<TextMeshPro>();
        text?.SetText(name);
        return button;
    }

    [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.Update))]
    [HarmonyPrefix]
    private static void PlayerTab_Updatee_Postfix(PlayerTab __instance)
    {
        // Handle cooldown
        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
        }
        else
        {
            cooldown = 0f;
        }

        // Update preset button hover states
        for (int i = 0; i < presetButtons.Count; i++)
        {
            PassiveButton? button = presetButtons[i];
            if (button == null) continue;
            if (i == BetterDataManager.BetterDataFile.SelectedOutfitPreset)
            {
                button.SetPassiveButtonHoverStateActive(); // Highlight selected preset
            }
        }
    }
}