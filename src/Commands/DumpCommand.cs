using BetterAmongUs.Attributes;
using BetterAmongUs.Data;
using BetterAmongUs.Modules;

namespace BetterAmongUs.Commands;

[RegisterCommand]
internal sealed class DumpCommand : BaseCommand
{
    internal override string Name => "dump";
    internal override string Description => "Dump the entire log to the user's desktop";
    internal override bool ShowCommand() => !ModInfo.Starlight;

    internal override bool CanRunCommand(out string reason)
    {
        if (GameState.IsInGamePlay)
        {
            reason = "Only can run in lobby";
            return false;
        }

        return base.CanRunCommand(out reason);
    }

    internal override void Run()
    {
        string logFilePath = Path.Combine(BetterDataManager.filePathFolder, "better-log.txt");
        string log = File.ReadAllText(logFilePath);
        string newLog = string.Empty;
        string[] logArray = log.Split([Environment.NewLine], StringSplitOptions.None);
        foreach (string text in logArray)
        {
            if (text.Contains("[PrivateLog]"))
            {
                newLog += text.Split(':')[0] + ":" + text.Split(':')[1].Replace("[PrivateLog]", "") + ": " + Encryptor.Decrypt(text.Split(':')[2][1..]) + "\n";
            }
            else
            {
                newLog += text + "\n";
            }
        }

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string logFolderPath = Path.Combine(desktopPath, "BetterLogs");
        if (!Directory.Exists(logFolderPath))
        {
            Directory.CreateDirectory(logFolderPath);
        }
        string logFileName = "log-" + BAUPlugin.GetVersionText().Replace(' ', '-').ToLower() + "-" + DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss") + ".log";
        string newLogFilePath = Path.Combine(logFolderPath, logFileName);
        File.WriteAllText(newLogFilePath, newLog);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
            FileName = logFolderPath,
            UseShellExecute = true,
            Verb = "open"
        });
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
            FileName = newLogFilePath,
            UseShellExecute = true,
            Verb = "open"
        });

        CommandResultText($"Dump logs at <color=#b1b1b1>'{newLogFilePath}'</color>");
    }
}
