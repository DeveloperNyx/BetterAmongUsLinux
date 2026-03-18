using BetterAmongUs.Attributes;
using BetterAmongUs.Commands.Arguments;
using BetterAmongUs.Data;
using BetterAmongUs.Helpers;

namespace BetterAmongUs.Commands;

[RegisterCommand]
internal sealed class RemovePlayerCommand : BaseCommand
{
    internal override string Name => "removeplayer";
    internal override string Description => "Remove player from local <color=#4f92ff>Anti-Cheat</color> data";

    public RemovePlayerCommand()
    {
        _identifierArgument = new StringArgument(this, "{identifier}")
        {
            GetArgSuggestions = () =>
                BetterDataManager.BetterDataFile.AllCheatData
                    .SelectMany(info => new[] { info.HashPuid.Replace(' ', '_'), info.FriendCode.Replace(' ', '_'), info.PlayerName.Replace(' ', '_') })
                    .ToArray()
        };
        Arguments = [_identifierArgument];
    }
    private readonly StringArgument _identifierArgument;

    internal override void Run()
    {
        if (BetterDataManager.RemovePlayer(_identifierArgument.Arg) == true)
        {
            Utils.AddChatPrivate($"{_identifierArgument.Arg} successfully removed from local <color=#4f92ff>Anti-Cheat</color> data!");
        }
        else
        {
            Utils.AddChatPrivate($"{_identifierArgument.Arg} Could not find player data from identifier");
        }
    }
}
