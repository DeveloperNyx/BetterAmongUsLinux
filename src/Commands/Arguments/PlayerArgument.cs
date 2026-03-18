using BetterAmongUs.Helpers;

namespace BetterAmongUs.Commands.Arguments;

/// <summary>
/// Represents a player command argument.
/// </summary>
/// <param name="command">The command this argument belongs to.</param>
/// <param name="argInfo">Information about the argument (default: "{player}").</param>
internal sealed class PlayerArgument(BaseCommand command, string argInfo = "{player}") : BaseArgument(command, argInfo)
{
    /// <summary>
    /// Gets the player argument suggestions for auto-completion.
    /// </summary>
    /// <remarks>
    /// Suggestions are ordered with the local player first, then other players.
    /// Spaces in player names are replaced with underscores for command parsing.
    /// </remarks>
    protected override string[] ArgSuggestions => BAUPlugin.AllPlayerControls.OrderBy(pc => pc.IsLocalPlayer() ? 0 : 1).Select(pc => pc.Data.PlayerName.Replace(' ', '_')).ToArray();

    /// <summary>
    /// Attempts to get a PlayerControl instance based on the argument value.
    /// </summary>
    /// <returns>
    /// The PlayerControl if found; otherwise, null.
    /// Displays an error message if the player is not found.
    /// </returns>
    internal PlayerControl? TryGetTarget()
    {
        var player = BAUPlugin.AllPlayerControls.FirstOrDefault(pc => pc.Data.PlayerName.ToLower().Replace(' ', '_') == Arg.ToLower());

        if (player == null)
        {
            BaseCommand.CommandErrorText($"Player not found!");
        }

        return player;
    }
}