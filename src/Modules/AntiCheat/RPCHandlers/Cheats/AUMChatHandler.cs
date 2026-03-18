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

namespace BetterAmongUs.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class AUMChatHandler : RPCHandler
{
    internal override byte CallId => unchecked((byte)CustomRPC.AUMChat);

    internal override void HandleCheatRpcCheck(PlayerControl? sender, MessageReader reader)
    {
        var nameString = reader.ReadString();
        var msgString = reader.ReadString();
        var colorId = reader.ReadInt32();

        var flag3 = sender.BetterData().AntiCheatInfo.AUMChats.Count > 0 && sender.BetterData().AntiCheatInfo.AUMChats.Last() == msgString;
        if (!flag3)
        {
            Utils.AddChatPrivate($"{msgString}", overrideName: $"<b><color=#870000>AUM Chat</color> - {sender.GetPlayerNameAndColor()}</b>");
            sender.BetterData().AntiCheatInfo.AUMChats.Add(msgString);
        }

        Logger_.Log($"{sender.Data.PlayerName} -> {msgString}", "AUMChatLog");

        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_Anticheat))
            return;

        if (!BAUConfigs.AntiCheat.Value || !BetterGameSettings.DetectCheatClients.GetBool())
            return;

        var flag = string.IsNullOrEmpty(nameString) && string.IsNullOrEmpty(msgString);

        if (!flag && !BetterDataManager.BetterDataFile.AUMData.Any(info => info.CheckPlayerData(sender.Data)))
        {
            sender.ReportPlayer(ReportReasons.Cheating_Hacking);
            BetterDataManager.BetterDataFile.AUMData.Add(new(sender?.BetterData().RealName ?? sender.Data.PlayerName, sender.GetHashPuid(), sender.Data.FriendCode, "AUM Chat RPC"));
            BetterDataManager.BetterDataFile.Save();
            BetterNotificationManager.NotifyCheat(sender, Translator.GetString("AntiCheat.Cheat.AUM"), Translator.GetString("AntiCheat.HasBeenDetectedWithCheat2"));
        }
    }
}