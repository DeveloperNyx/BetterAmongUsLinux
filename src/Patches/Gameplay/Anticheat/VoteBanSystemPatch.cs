using BetterAmongUs.Helpers;
using BetterAmongUs.Managers;
using BetterAmongUs.Modules;
using BetterAmongUs.Modules.Support;
using HarmonyLib;

namespace BetterAmongUs.Patches.Gameplay.Anticheat;

[HarmonyPatch]
internal static class VoteBanSystemPatch
{
    private static readonly Dictionary<VoteBanSystem, List<(int ClientId, (ushort HashPuid, string FriendCode) Voter)>> _voteData = [];

    private static bool DoLog;

    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
    [HarmonyPrefix]
    private static bool VoteBanSystem_AddVote_Prefix(VoteBanSystem __instance, int srcClient, int clientId)
    {
        // Skip BAU anti-cheat if disabled by other mods
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_Anticheat)) return true;

        // If not host, allow vote and log it
        if (!GameState.IsHost)
        {
            DoLog = true;
            return true;
        }

        var client = Utils.ClientFromClientId(srcClient);
        if (client == null) return false;

        // Allow host to vote without restrictions
        if (client.Id == AmongUsClient.Instance.GetHost().Id)
        {
            return true;
        }

        void TryFlagPlayer()
        {
            var player = client.Character;
            if (player != null)
            {
                BetterNotificationManager.NotifyCheat(player, string.Format(Translator.GetString("AntiCheat.InvalidLobbyRPC"), "VoteKick"));
            }
        }

        // Prevent voting in lobby (anti-cheat measure)
        if (GameState.IsLobby)
        {
            TryFlagPlayer();
            return false;
        }

        // Initialize vote tracking for this VoteBanSystem instance
        if (!_voteData.TryGetValue(__instance, out var voters))
        {
            _voteData.Clear();
            _voteData[__instance] = voters = [];
        }

        // If client has no ID, allow vote but log it
        if (string.IsNullOrEmpty(client.ProductUserId) && string.IsNullOrEmpty(client.FriendCode))
        {
            DoLog = true;
            return true;
        }

        // Generate hash for client's ProductUserId
        var clientHash = Utils.GetHashUInt16(client.ProductUserId);

        // Check if this client has already voted for the same target
        foreach (var (targetClientId, (existingHash, existingFriendCode)) in voters)
        {
            if (targetClientId != clientId)
                continue;

            // Detect duplicate votes by comparing hash or friend code
            bool isDuplicateVote = existingHash == clientHash ||
                                  !string.IsNullOrEmpty(client.FriendCode) &&
                                   existingFriendCode == client.FriendCode;

            if (isDuplicateVote)
            {
                // Block duplicate vote attempt
                return false;
            }
        }

        // Record the vote
        voters.Add((clientId, (clientHash, client.FriendCode)));
        DoLog = true;
        return true;
    }

    [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.AddVote))]
    [HarmonyPostfix]
    private static void VoteBanSystem_AddVote_Postfix(VoteBanSystem __instance, int srcClient, int clientId)
    {
        // Skip logging if anti-cheat disabled
        if (BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Disable_Anticheat))
            return;

        // Log the vote if it was allowed
        if (DoLog)
        {
            LogVote(__instance, srcClient, clientId);
            DoLog = false;
        }
    }

    private static void LogVote(VoteBanSystem voteBanSystem, int srcClient, int clientId)
    {
        // Get source and target client info
        var src = Utils.ClientFromClientId(srcClient);
        var client = Utils.ClientFromClientId(clientId);

        // Calculate current votes and required votes
        int currentVotes = 0;
        int maxVotes = 0;

        if (voteBanSystem.Votes.TryGetValue(clientId, out var votes))
        {
            currentVotes = votes.Count(v => v != 0); // Count non-zero votes
            maxVotes = votes.Length; // Total possible votes
        }

        // Log vote with colored player names and vote count
        Logger_.InGame(
            $"{src.Character?.GetPlayerNameAndColor() ?? src.PlayerName} " +
            $"voted to kick {client.Character?.GetPlayerNameAndColor() ?? client.PlayerName} " +
            $"<#6F6F6F>(</color><#FFFFFF>{currentVotes}</color><#6F6F6F>/</color><#FFFFFF>{maxVotes}</color><#6F6F6F>)</color>"
        );
    }
}