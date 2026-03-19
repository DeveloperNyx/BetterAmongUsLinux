using BetterAmongUs.Attributes;
using BetterAmongUs.Data;
using BetterAmongUs.Data.Config;
using BetterAmongUs.Enums;
using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Modules.Support;
using BetterAmongUs.Mono;
using BetterAmongUs.Patches.Gameplay.UI.Settings;
using Hazel;
using InnerNet;

namespace BetterAmongUs.Modules.AntiCheat.RPCHandlers.Cheats;

[RegisterRPCHandler]
internal sealed class AUMChatHandler : RPCHandler
{
    internal override byte CallId => unchecked((byte)CustomRPC.AUMChat);

    internal override void HandleCheatRpcCheck(PlayerControl? sender, MessageReader reader)
    {
        try
        {
            var nameString = reader.ReadString();
            var msgString = reader.ReadString();
            var colorId = reader.ReadInt32();

            var betterData = sender.BetterData();
            var alreadyContainsMessage = betterData.AntiCheatInfo.AUMChats.Count > 0 && betterData.AntiCheatInfo.AUMChats.Last() == msgString;
            if (!alreadyContainsMessage)
            {
                Utils.AddChatPrivate($"{msgString}", overrideName: $"<b>{Translator.GetString("AntiCheat.Cheat.AUMChat").ToColor(Colors.AUMHexColor)} - {sender.GetPlayerNameAndColor()}</b>");
                betterData.AntiCheatInfo.AUMChats.Add(msgString);
            }

            Logger_.Log($"{sender.Data.PlayerName} -> {msgString}", "AUMChatLog");

            if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_Anticheat))
                return;

            if (!BAUConfigs.AntiCheat.Value || !BetterGameSettings.DetectCheatClients.GetBool())
                return;

            var isEmpty = string.IsNullOrEmpty(nameString) && string.IsNullOrEmpty(msgString);

            if (!isEmpty && !BetterDataManager.BetterDataFile.AUMData.Any(info => info.CheckPlayerData(sender.Data)))
            {
                sender.ReportPlayer(ReportReasons.Cheating_Hacking);
                BetterDataManager.BetterDataFile.AUMData.Add(new(betterData.RealName ?? sender.Data.PlayerName, sender.GetHashPuid(), sender.Data.FriendCode, "AUM Chat RPC"));
                BetterDataManager.BetterDataFile.Save();
                BetterNotificationManager.NotifyCheat(sender, Translator.GetString("AntiCheat.Cheat.AUMChat"), Translator.GetString("AntiCheat.HasBeenDetectedWithCheatClient"));
            }
        }
        catch
        {
            if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_Anticheat))
                return;

            if (!BAUConfigs.AntiCheat.Value || !BetterGameSettings.DetectCheatClients.GetBool())
                return;

            if (!BetterDataManager.BetterDataFile.AUMData.Any(info => info.CheckPlayerData(sender.Data)))
            {
                sender.ReportPlayer(ReportReasons.Cheating_Hacking);
                BetterDataManager.BetterDataFile.AUMData.Add(new(sender?.BetterData().RealName ?? sender.Data.PlayerName, sender.GetHashPuid(), sender.Data.FriendCode, "AUM Chat RPC"));
                BetterDataManager.BetterDataFile.Save();
                BetterNotificationManager.NotifyCheat(sender, Translator.GetString("AntiCheat.Cheat.AUMChat"), Translator.GetString("AntiCheat.HasBeenDetectedWithCheatClient"));
            }
        }
    }
}