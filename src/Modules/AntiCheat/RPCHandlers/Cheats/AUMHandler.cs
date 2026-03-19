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
internal sealed class AUMHandler : RPCHandler
{
    internal override byte CallId => unchecked((byte)CustomRPC.AUM);

    internal override void HandleCheatRpcCheck(PlayerControl? sender, MessageReader reader)
    {
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_Anticheat))
            return;

        if (!BAUConfigs.AntiCheat.Value || !BetterGameSettings.DetectCheatClients.GetBool())
            return;

        try
        {
            var aumid = reader.ReadByte();

            if (aumid == sender.PlayerId)
            {
                if (!BetterDataManager.BetterDataFile.AUMData.Any(info => info.CheckPlayerData(sender.Data)))
                {
                    sender.ReportPlayer(ReportReasons.Cheating_Hacking);
                    BetterDataManager.BetterDataFile.AUMData.Add(new(sender?.BetterData().RealName ?? sender.Data.PlayerName, sender.GetHashPuid(), sender.Data.FriendCode, "AUM RPC"));
                    BetterDataManager.BetterDataFile.Save();
                    BetterNotificationManager.NotifyCheat(sender, Translator.GetString("AntiCheat.Cheat.AUM"), Translator.GetString("AntiCheat.HasBeenDetectedWithCheatClient"));
                }
            }
        }
        catch
        {
            if (!BetterDataManager.BetterDataFile.AUMData.Any(info => info.CheckPlayerData(sender.Data)))
            {
                sender.ReportPlayer(ReportReasons.Cheating_Hacking);
                BetterDataManager.BetterDataFile.AUMData.Add(new(sender?.BetterData().RealName ?? sender.Data.PlayerName, sender.GetHashPuid(), sender.Data.FriendCode, "AUM RPC"));
                BetterDataManager.BetterDataFile.Save();
                BetterNotificationManager.NotifyCheat(sender, Translator.GetString("AntiCheat.Cheat.AUM"), Translator.GetString("AntiCheat.HasBeenDetectedWithCheatClient"));
            }
        }
    }
}