namespace BetterAmongUs.Commands.Arguments;

/// <summary>
/// Represents a boolean command argument.
/// </summary>
/// <param name="command">The command this argument belongs to.</param>
/// <param name="argInfo">Information about the argument (default: "{bool}").</param>
internal sealed class BoolArgument(BaseCommand command, string argInfo = "{bool}") : BaseArgument(command, argInfo)
{
    /// <summary>
    /// Gets the boolean argument suggestions for auto-completion.
    /// </summary>
    protected override string[] ArgSuggestions => ["true", "false"];

    /// <summary>
    /// Parses the argument string to a nullable boolean value.
    /// </summary>
    /// <returns>
    /// True if the argument is "true",
    /// False if the argument is "false" or empty,
    /// Null if the argument is invalid.
    /// </returns>
    /// <remarks>
    /// Displays an error message if the argument is invalid.
    /// </remarks>
    internal bool? GetBool()
    {
        if (Arg.ToLower() == "true")
        {
            return true;
        }
        else if (Arg.ToLower() is "false" or "")
        {
            return false;
        }
        else
        {
            BaseCommand.CommandErrorText($"Invalid Syntax!");
        }

        return null;
    }
}