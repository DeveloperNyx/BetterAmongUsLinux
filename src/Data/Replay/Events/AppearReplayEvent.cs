using AmongUs.GameOptions;
using BetterAmongUs.Helpers;
using BetterAmongUs.Interfaces;
using System.Text.Json.Serialization;

namespace BetterAmongUs.Data.Replay.Events;

[Serializable]
internal sealed class AppearReplayEvent : IReplayEvent<(int playerId, bool animate)>
{
    public string Id => "player_appear";

    [JsonInclude]
    public (int playerId, bool animate) EventData { get; set; }

    public void Play()
    {
        var player = Utils.PlayerFromPlayerId(EventData.playerId);
        if (player == null)
            return;

        if (player.Data.RoleType != RoleTypes.Phantom)
            return;

        player.SetRoleInvisibility(false, EventData.animate, true);
    }

    public void Record(PlayerControl player, bool animate)
    {
        EventData = (player.PlayerId, animate);
    }
}
