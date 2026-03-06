using AmongUs.Data;
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
    private static List<PoolablePlayer> presetPreviews = []; // Outfit previews
    private static float cooldown = 0f; // Button click cooldown
    private static readonly List<SpriteRenderer> _favoriteIcons = [];

    [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.OnEnable))]
    [HarmonyPrefix]
    private static void PlayerTab_OnEnable_Prefix(PlayerTab __instance)
    {
        SetupOutfitPresets(__instance);
    }

    [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.OnEnable))]
    [HarmonyPostfix]
    private static void PlayerTab_OnEnable_Postfix(PlayerTab __instance)
    {
        SetupFavoriteColor(__instance);
    }

    private static void SetupOutfitPresets(PlayerTab playerTab)
    {
        // Clean up old preset buttons and previews
        foreach (var button in presetButtons.ToArray())
        {
            if (button == null) continue;
            UnityEngine.Object.Destroy(button.gameObject);
        }
        presetButtons.Clear();
        foreach (var preview in presetPreviews.ToArray())
        {
            if (preview == null) continue;
            UnityEngine.Object.Destroy(preview.gameObject);
        }
        presetPreviews.Clear();

        // Create preset buttons (0 = Among Us preset, 1-5 = custom presets)
        for (int i = 0; i <= 5; i++)
        {
            int currentI = i;
            var name = currentI == 0 ? "Among Us Preset" : $"Preset {i}";
            var data = OutfitData.GetOutfitData(currentI);
            var button = playerTab.CreateOutfitPresetButton(name, new Vector3(2.5f, 1.55f - currentI * 0.45f, 0f), out var playerPreview, () =>
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
                data.Load(() =>
                {
                    if (LoadPlayerOutfit(data))
                    {
                        playerTab.PlayerPreview.UpdateFromLocalPlayer(PlayerMaterial.MaskType.None);
                    }
                    else
                    {
                        playerTab.PlayerPreview.UpdateFromDataManager(PlayerMaterial.MaskType.None);
                    }
                });
            });

            // Set up preview
            playerPreview.UpdateFromPlayerOutfit(data.ToPlayerOutfit(), PlayerMaterial.MaskType.None, false, true);
            playerPreview.ToggleName(false);
            playerPreview.transform.position += new Vector3(0f, 0f, -1f * currentI); // Fix rendering order
            presetPreviews.Add(playerPreview);

            presetButtons.Add(button);
        }
    }

    private static void SetupFavoriteColor(PlayerTab playerTab)
    {
        _favoriteIcons.Clear();

        // Add favorite functionality to color chips
        for (int i = 0; i < playerTab.ColorChips.Count; i++)
        {
            var index = i;
            var colorChip = playerTab.ColorChips[i];

            // Override click behavior
            colorChip.Button.OnClick = new();
            colorChip.Button.OnClick.AddListener(() =>
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
                playerTab.ClickEquip();
            });

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
    private static PassiveButton CreateOutfitPresetButton(this PlayerTab __instance, string name, Vector3 pos, out PoolablePlayer playerPreview, Action callback)
    {
        var button = UnityEngine.Object.Instantiate(MainMenuManagerPatch.ButtonPrefab, __instance.transform);
        button.gameObject.SetActive(true);
        button.gameObject.SetLayers("UI");
        button.transform.localPosition = pos;
        button.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        // Create preview
        button.transform.Find("Highlight/Icon")?.gameObject.DestroyObj();
        button.transform.Find("Inactive/Icon")?.gameObject.DestroyObj();
        var outfitPreview = UnityEngine.Object.Instantiate(__instance.PlayerPreview);
        outfitPreview.transform.SetParent(button.transform);
        outfitPreview.transform.localPosition = new Vector3(-1.3f, 0.15f, -10f);
        outfitPreview.transform.localScale = Vector3.one * 0.45f;
        outfitPreview.ResetCosmetics();
        foreach (var pet in outfitPreview.GetComponentsInChildren<PetBehaviour>(true))
        {
            pet.gameObject.DestroyObj();
        }
        playerPreview = outfitPreview;

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

            // Update preview color
            var preview = presetPreviews[i];
            if (preview.Cosmetics.ColorId != DataManager.Player.Customization.Color)
            {
                preview.SetBodyColor(DataManager.Player.Customization.Color);
            }
        }
    }
}