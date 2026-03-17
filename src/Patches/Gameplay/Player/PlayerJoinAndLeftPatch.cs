using BetterAmongUs.Data;
using BetterAmongUs.Data.Config;
using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using BetterAmongUs.Mono;
using BetterAmongUs.Patches.Gameplay.UI;
using BetterAmongUs.Patches.Gameplay.UI.Settings;
using HarmonyLib;
using InnerNet;

namespace BetterAmongUs.Patches.Gameplay.Player;

[HarmonyPatch]
internal static class PlayerJoinAndLeftPatch
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    [HarmonyPostfix]
    private static void AmongUsClient_OnGameJoined_Postfix()
    {
        // Fix host icon color display on modded servers
        if (!GameState.IsVanillaServer)
        {
            var host = AmongUsClient.Instance.GetHost().Character;
            host?.SetColor(-2);
            host?.SetColor(host.CurrentOutfit.ColorId);
        }

        Logger_.Log($"Successfully joined {GameCode.IntToGameName(AmongUsClient.Instance.GameId)}", "OnGameJoinedPatch");
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    [HarmonyPostfix]
    private static void AmongUsClient_OnPlayerJoined_Postfix(ClientData data)
    {
        // Schedule ban list checks 2.5 seconds after player joins
        LateTask.Schedule(() =>
        {
            if (GameState.IsHost)
            {
                if (GameState.IsInGame)
                {
                    var player = Utils.PlayerFromClientId(data.Id);

                    // Check if player is in ban list by friend code or PUID
                    if (BetterGameSettings.UseBanPlayerList.GetBool())
                    {
                        if (player != null)
                        {
                            if (TextFileHandler.CompareStringMatch(BetterDataManager.banPlayerListFile,
                                BAUPlugin.AllPlayerControls.Select(player => player.Data.FriendCode)
                                .Concat(BAUPlugin.AllPlayerControls.Select(player => player.GetHashPuid())).ToArray()))
                            {
                                player.Kick(true, Translator.GetString("AntiCheat.BanPlayerListMessage"), bypassDataCheck: true);
                            }
                        }
                    }

                    // Check if player name matches banned name patterns
                    if (BetterGameSettings.UseBanNameList.GetBool())
                    {
                        if (player != null)
                        {
                            if (TextFileHandler.CompareStringFilters(BetterDataManager.banNameListFile, [player.Data.PlayerName]))
                            {
                                player?.Kick(true, Translator.GetString("AntiCheat.BanPlayerListMessage"), bypassDataCheck: true);
                            }
                        }
                    }
                }
            }
        }, 2.5f, "OnPlayerJoinedPatch", false);
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    [HarmonyPostfix]
    private static void AmongUsClient_OnPlayerLeft_Postfix(ClientData data, DisconnectReasons reason)
    {
        // Reclaim favorite color when player leaves in lobby
        if (GameState.IsLobby)
        {
            var favColorId = (byte)BAUConfigs.FavoriteColor.Value;
            if (BAUConfigs.FavoriteColor.Value >= 0)
            {
                if (PlayerControl.LocalPlayer.cosmetics.ColorId != favColorId && data.ColorId == favColorId)
                {
                    PlayerControl.LocalPlayer.CmdCheckColor(favColorId);
                }
            }
        }

        // Update host icon in meeting
        MeetingHudPatch.UpdateHostIcon();
    }

    [HarmonyPatch(typeof(GameData))]
    [HarmonyPatch(nameof(GameData.HandleDisconnect))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch([typeof(PlayerControl), typeof(DisconnectReasons)])]
    [HarmonyPrefix]
    private static void GameData_HandleDisconnect_Prefix(PlayerControl player, DisconnectReasons reason)
    {
        // Store disconnect reason in player's BetterData
        if (player.BetterData() != null)
        {
            player.BetterData().DisconnectReason = reason;
        }

        // Show custom disconnect notification
        BetterShowNotification(player.Data, reason);
    }

    [HarmonyPatch(typeof(GameData), nameof(GameData.ShowNotification))]
    [HarmonyPrefix]
    internal static bool GameData_ShowNotification_Prefix()
    {
        // Disable vanilla disconnect notifications (use BAU's instead)
        return false;
    }

    internal static void BetterShowNotification(NetworkedPlayerInfo playerData, DisconnectReasons reason = DisconnectReasons.Unknown, string forceReasonText = "")
    {
        // Prevent showing duplicate notifications
        if (playerData.BetterData().AntiCheatInfo.BannedByAntiCheat || playerData.BetterData().HasShowDcMsg) return;
        playerData.BetterData().HasShowDcMsg = true;

        string? playerName = playerData.BetterData().RealName;

        // Use custom reason text if provided
        if (forceReasonText != "")
        {
            var ReasonText = $"<color=#ff0>{playerData.BetterData().RealName}</color> {forceReasonText}";

            Logger_.Log(ReasonText);

            HudManager.Instance.Notifier.AddDisconnectMessage(ReasonText);
        }
        else
        {
            string ReasonText;

            // Format disconnect message based on reason type
            switch (reason)
            {
                case DisconnectReasons.ExitGame:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Left"), playerName);
                    break;
                case DisconnectReasons.ClientTimeout:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Disconnect"), playerName);
                    break;
                case DisconnectReasons.Kicked:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Kicked"), playerName, AmongUsClient.Instance.GetHost().Character.Data.PlayerName);
                    break;
                case DisconnectReasons.Banned:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Banned"), playerName, AmongUsClient.Instance.GetHost().Character.Data.PlayerName);
                    break;
                case DisconnectReasons.Hacking:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Cheater"), playerName);
                    break;
                case DisconnectReasons.Error:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Error"), playerName);
                    break;
                case DisconnectReasons.Unknown:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Unknown"), playerName);
                    break;
                default:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Left"), playerName);
                    break;
            }

            Logger_.Log(ReasonText);

            // Add formatted disconnect message to game UI
            HudManager.Instance.Notifier.AddDisconnectMessage(ReasonText);
        }
    }
}