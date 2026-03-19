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
internal sealed class ModMenuCrewChatHandler : RPCHandler
{
    internal override byte CallId => unchecked((byte)CustomRPC.ModMenuCrewChat);

    internal override void HandleCheatRpcCheck(PlayerControl? sender, MessageReader reader)
    {
        try
        {
            var tag = reader.ReadByte();
            var senderId = reader.ReadPackedInt32();
            var senderName = reader.ReadString();
            var content = reader.ReadString();
            var timestamp = reader.ReadUInt64();
            var type = reader.ReadByte();

            if (sender.PlayerId != senderId)
                return;

            var betterData = sender.BetterData();
            var alreadyContainsMessage = betterData.AntiCheatInfo.MCCChats.Count > 0 && betterData.AntiCheatInfo.MCCChats.Last() == content;
            if (!alreadyContainsMessage)
            {
                Utils.AddChatPrivate($"{content}", overrideName: $"<b>{Translator.GetString("AntiCheat.Cheat.MMCChat").ToColor(Colors.MMCHexColor)} - {sender.GetPlayerNameAndColor()}</b>");
                betterData.AntiCheatInfo.MCCChats.Add(content);
            }

            if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_Anticheat))
                return;

            if (!BAUConfigs.AntiCheat.Value || !BetterGameSettings.DetectCheatClients.GetBool())
                return;

            var isEmpty = string.IsNullOrEmpty(senderName) && string.IsNullOrEmpty(content);
            if (!isEmpty && !BetterDataManager.BetterDataFile.MMCData.Any(info => info.CheckPlayerData(sender.Data)))
            {
                sender.ReportPlayer(ReportReasons.Cheating_Hacking);
                BetterDataManager.BetterDataFile.MMCData.Add(new(sender?.BetterData().RealName ?? sender.Data.PlayerName, sender.GetHashPuid(), sender.Data.FriendCode, "ModMenuCrew Chat RPC"));
                BetterDataManager.BetterDataFile.Save();
                BetterNotificationManager.NotifyCheat(sender, Translator.GetString("AntiCheat.Cheat.MMCChat"), Translator.GetString("AntiCheat.HasBeenDetectedWithCheatClient"));
            }
        }
        catch
        {
            if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_Anticheat))
                return;

            if (!BAUConfigs.AntiCheat.Value || !BetterGameSettings.DetectCheatClients.GetBool())
                return;

            if (!BetterDataManager.BetterDataFile.MMCData.Any(info => info.CheckPlayerData(sender.Data)))
            {
                sender.ReportPlayer(ReportReasons.Cheating_Hacking);
                BetterDataManager.BetterDataFile.MMCData.Add(new(sender?.BetterData().RealName ?? sender.Data.PlayerName, sender.GetHashPuid(), sender.Data.FriendCode, "ModMenuCrew Chat RPC"));
                BetterDataManager.BetterDataFile.Save();
                BetterNotificationManager.NotifyCheat(sender, Translator.GetString("AntiCheat.Cheat.MMCChat"), Translator.GetString("AntiCheat.HasBeenDetectedWithCheatClient"));
            }
        }
    }
}