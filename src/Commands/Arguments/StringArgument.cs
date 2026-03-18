namespace BetterAmongUs.Commands.Arguments;

/// <summary>
/// Represents a string command argument.
/// </summary>
/// <param name="command">The command this argument belongs to.</param>
/// <param name="argInfo">Information about the argument (default: "{String}").</param>
internal sealed class StringArgument(BaseCommand command, string argInfo = "{String}") : BaseArgument(command, argInfo)
{
    // This class inherits the basic string argument functionality from BaseArgument
    // and can be extended with string-specific parsing or validation if needed.
}