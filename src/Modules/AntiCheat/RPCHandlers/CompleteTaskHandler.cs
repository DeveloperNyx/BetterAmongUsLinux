using BetterAmongUs.Attributes;
using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Mono;
using Hazel;

namespace BetterAmongUs.Modules.AntiCheat.RPCHandlers;

[RegisterRPCHandler]
internal sealed class CompleteTaskHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.CompleteTask;

    internal override void HandleAntiCheat(PlayerControl? sender, MessageReader reader)
    {
        var taskId = reader.ReadPackedUInt32();

        if (sender.IsImpostorTeam() || !sender.Data.Tasks.AnyIl2Cpp(task => task.Id == taskId)
            || sender.BetterData().AntiCheatInfo.LastTaskId == taskId || sender.BetterData().AntiCheatInfo.LastTaskId != taskId
            && sender.BetterData().AntiCheatInfo.TimeSinceLastTask < 1.25f)
        {
            if (BetterNotificationManager.NotifyCheat(sender, GetFormatActionText()))
            {
                string reason = GetCompleteTaskIssue(sender, taskId);
                LogRpcInfo($"Invalid task completion: {reason}");
            }
        }

        sender.BetterData().AntiCheatInfo.TimeSinceLastTask = 0f;
        sender.BetterData().AntiCheatInfo.LastTaskId = taskId;
    }

    private static string GetCompleteTaskIssue(PlayerControl sender, uint taskId)
    {
        if (sender.IsImpostorTeam()) return "Impostor completing tasks";
        if (!sender.Data.Tasks.AnyIl2Cpp(task => task.Id == taskId)) return $"Task ID {taskId} not assigned to player";
        if (sender.BetterData().AntiCheatInfo.LastTaskId == taskId) return $"Task ID {taskId} already completed";
        if (sender.BetterData().AntiCheatInfo.TimeSinceLastTask < 1.25f)
            return $"Tasks too fast (time since last: {sender.BetterData().AntiCheatInfo.TimeSinceLastTask}s)";

        return "Unknown task completion issue";
    }
}
