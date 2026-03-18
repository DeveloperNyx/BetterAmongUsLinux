using BetterAmongUs.Attributes;
using BetterAmongUs.Modules;

namespace BetterAmongUs.Commands;

[RegisterCommand]
internal sealed class EndGameCommand : BaseCommand
{
    internal override string Name => "endgame";
    internal override string Description => "Force end the game";
    internal override bool ShowCommand() =>
        GameState.IsHost && !GameState.IsLobby && !GameState.IsFreePlay;
    internal override void Run()
    {
        GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
    }
}
