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
internal sealed class SickoHandler : RPCHandler
{
    internal override byte CallId => unchecked((byte)CustomRPC.Sicko);

    internal override void HandleCheatRpcCheck(PlayerControl? sender, MessageReader reader)
    {
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_Anticheat))
            return;

        if (!BAUConfigs.AntiCheat.Value || !BetterGameSettings.DetectCheatClients.GetBool())
            return;

        if (reader.BytesRemaining == 0 && !BetterDataManager.BetterDataFile.SickoData.Any(info => info.CheckPlayerData(sender.Data)))
        {
            sender.ReportPlayer(ReportReasons.Cheating_Hacking);
            BetterDataManager.BetterDataFile.SickoData.Add(new(sender?.BetterData().RealName ?? sender.Data.PlayerName, sender.GetHashPuid(), sender.Data.FriendCode, "Sicko RPC"));
            BetterDataManager.BetterDataFile.Save();
            BetterNotificationManager.NotifyCheat(sender, Translator.GetString("AntiCheat.Cheat.Sicko"), newText: Translator.GetString("AntiCheat.HasBeenDetectedWithCheat2"));
        }
    }
}