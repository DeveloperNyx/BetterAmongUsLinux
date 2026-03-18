using BetterAmongUs.Attributes;
using BetterAmongUs.Commands.Arguments;
using BetterAmongUs.Data.Config;
using BetterAmongUs.Modules.Support;

namespace BetterAmongUs.Commands;

[RegisterCommand]
internal sealed class SetPrefixCommand : BaseCommand
{
    internal override string Name => "setprefix";
    internal override string Description => "Set command prefix";

    internal SetPrefixCommand()
    {
        _prefixArgument = new StringArgument(this, "{prefix}");
        Arguments = [_prefixArgument];
    }
    private readonly StringArgument _prefixArgument;

    internal override bool ShowCommand() =>
        !BAUModdedSupportFlags.HasFlag(BAUModdedSupportFlags.Force_BAU_Command_Prefix);

    internal override void Run()
    {
        var oldPrefix = BAUConfigs.CommandPrefix.Value;
        var prefix = _prefixArgument.Arg.ToCharArray()?.First().ToString();
        if (!string.IsNullOrEmpty(prefix))
        {
            BAUConfigs.CommandPrefix.Value = prefix;
            CommandResultText($"Command prefix set from <#c1c100>{oldPrefix}</color> to <#c1c100>{prefix}</color>");
        }
        else
        {
            CommandErrorText("Invalid Syntax!");
        }
    }
}
