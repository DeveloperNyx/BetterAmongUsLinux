using BetterAmongUs.Attributes;
using BetterAmongUs.Commands.Arguments;
using BetterAmongUs.Modules.Support;

namespace BetterAmongUs.Commands;

[RegisterCommand]
internal sealed class SetPrefixCommand : BaseCommand
{
    internal override string Name => "setprefix";
    internal override string Description => "Set command prefix";

    internal SetPrefixCommand()
    {
        prefixArgument = new StringArgument(this, "{prefix}");
        Arguments = [prefixArgument];
    }
    private StringArgument? prefixArgument { get; }

    internal override bool ShowCommand() => !BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Force_BAU_Command_Prefix);

    internal override void Run()
    {
        var oldPrefix = BAUPlugin.CommandPrefix.Value;
        var prefix = prefixArgument.Arg.ToCharArray()?.First().ToString();
        if (!string.IsNullOrEmpty(prefix))
        {
            BAUPlugin.CommandPrefix.Value = prefix;
            CommandResultText($"Command prefix set from <#c1c100>{oldPrefix}</color> to <#c1c100>{prefix}</color>");
        }
        else
        {
            CommandErrorText("Invalid Syntax!");
        }
    }
}
