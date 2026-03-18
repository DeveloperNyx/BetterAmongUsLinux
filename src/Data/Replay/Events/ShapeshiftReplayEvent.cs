using AmongUs.GameOptions;
using BetterAmongUs.Helpers;
using BetterAmongUs.Interfaces;
using System.Text.Json.Serialization;

namespace BetterAmongUs.Data.Replay.Events;

[Serializable]
internal sealed class ShapeshiftReplayEvent : IReplayEvent<(int playerId, int targetId, bool animate)>
{
    public string Id => "player_shapeshift";

    [JsonInclude]
    public (int playerId, int targetId, bool animate) EventData { get; set; }

    public void Play()
    {
        var player = Utils.PlayerFromPlayerId(EventData.playerId);
        if (player == null)
            return;

        var target = Utils.PlayerFromPlayerId(EventData.targetId);
        if (target == null)
            return;

        if (player.Data.RoleType != RoleTypes.Shapeshifter)
            return;

        player.Shapeshift(target, EventData.animate);
    }

    public void Record(PlayerControl killer, PlayerControl target, bool animate)
    {
        EventData = (killer.PlayerId, target.PlayerId, animate);
    }
}
