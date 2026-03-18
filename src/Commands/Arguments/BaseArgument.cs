namespace BetterAmongUs.Commands.Arguments;

/// <summary>
/// Abstract base class for command arguments in BetterAmongUs.
/// </summary>
/// <param name="command">The command this argument belongs to.</param>
/// <param name="argInfo">Information about the argument.</param>
internal abstract class BaseArgument(BaseCommand command, string argInfo)
{
    /// <summary>
    /// Gets the command this argument belongs to.
    /// </summary>
    internal BaseCommand Command { get; } = command;

    /// <summary>
    /// Gets information about the argument.
    /// </summary>
    internal string ArgInfo { get; } = argInfo;

    /// <summary>
    /// Gets or sets the current argument value.
    /// </summary>
    internal string Arg { get; set; } = string.Empty;

    /// <summary>
    /// Gets the argument suggestions for auto-completion.
    /// </summary>
    protected virtual string[] ArgSuggestions => GetArgSuggestions.Invoke();

    /// <summary>
    /// Gets or sets the function that provides argument suggestions.
    /// </summary>
    internal Func<string[]> GetArgSuggestions { get; set; } = () => { return []; };

    /// <summary>
    /// Gets the closest suggestion for the current argument value.
    /// </summary>
    /// <returns>The closest matching suggestion, or empty string if none found.</returns>
    internal string GetClosestSuggestion() =>
        ArgSuggestions.FirstOrDefault(name => name.StartsWith(Arg, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
}