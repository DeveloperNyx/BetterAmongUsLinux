using BetterAmongUs.Attributes;
using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Mono;
using Hazel;

namespace BetterAmongUs.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class CheckNameHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.CheckName;

    internal override bool HandleAntiCheatCancel(PlayerControl? sender, MessageReader reader)
    {
        if (!GameState.IsHost)
        {
            return false;
        }

        var name = reader.ReadString();

        if (sender.DataIsCollected() == true && sender.BetterData().AntiCheatInfo.HasSetName && !GameState.IsLocalGame && GameState.IsVanillaServer)
        {
            if (BetterNotificationManager.NotifyCheat(sender, GetFormatSetText()))
            {
                Utils.AddChatPrivate($"{sender.GetPlayerNameAndColor()} Has tried to change their name to '{name}' but has been undone!");
                Logger_.LogCheat($"{sender.BetterData().RealName} Has tried to change their name to '{name}' but has been undone!");
                LogRpcInfo($"{sender.DataIsCollected() == true} && {!GameState.IsLocalGame} && {GameState.IsVanillaServer}");
            }

            return false;
        }

        sender.BetterData().AntiCheatInfo.HasSetName = true;

        return true;
    }
}