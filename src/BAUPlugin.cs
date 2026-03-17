#pragma warning disable CS0162

using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BetterAmongUs.Attributes;
using BetterAmongUs.Data;
using BetterAmongUs.Data.Config;
using BetterAmongUs.Data.Json;
using BetterAmongUs.Enums;
using BetterAmongUs.Helpers;
using BetterAmongUs.Modules;
using BetterAmongUs.Modules.OptionItems;
using BetterAmongUs.Modules.Support;
using BetterAmongUs.Network;
using BetterAmongUs.Patches.Gameplay.UI.Settings;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System.Reflection;
using UnityEngine;

namespace BetterAmongUs;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
[BepInProcess(ModInfo.AmongUs.PROCESS_NAME)]
internal class BAUPlugin : BasePlugin
{
    /// <summary>
    /// Gets the formatted version text for display.
    /// </summary>
    /// <param name="newLine">Whether to use newline separation for additional info.</param>
    /// <returns>Formatted version string.</returns>
    internal static string GetVersionText(bool newLine = false)
    {
        string text = string.Empty;

        string newLineText = newLine ? "\n" : " ";

        switch (ModInfo.ReleaseBuildType)
        {
            case ReleaseTypes.Release:
                text = $"v{BetterAmongUsVersion}";
                break;
            case ReleaseTypes.Beta:
                text = $"v{BetterAmongUsVersion}{newLineText}Beta {ModInfo.BETA_NUM}";
                break;
            case ReleaseTypes.Dev:
                text = $"v{BetterAmongUsVersion}{newLineText}Dev {ModInfo.CommitHash}-{ModInfo.BuildDate}";
                break;
            default:
                break;
        }

        if (ModInfo.IS_HOTFIX)
            text += $"{newLineText}Hotfix {ModInfo.HOTFIX_NUM}";

        return text;
    }

    /// <summary>
    /// Gets the BAUPlugin instance.
    /// </summary>
    internal static BAUPlugin? Instance { get; private set; }

    /// <summary>
    /// Gets the Harmony instance used for patching.
    /// </summary>
    internal static Harmony Harmony { get; } = new Harmony(ModInfo.PLUGIN_GUID);

    /// <summary>
    /// Gets the BetterAmongUs version string.
    /// </summary>
    internal static string BetterAmongUsVersion => ModInfo.PLUGIN_VERSION;

    /// <summary>
    /// Gets the application version string.
    /// </summary>
    internal static string AppVersion => Application.version;

    /// <summary>
    /// Gets the Among Us version string from reference data.
    /// </summary>
    internal static string AmongUsVersion => ReferenceDataManager.Instance.Refdata.userFacingVersion;

    /// <summary>
    /// Gets platform-specific data.
    /// </summary>
    internal static PlatformSpecificData PlatformData => Constants.GetPlatformData();

    /// <summary>
    /// Gets the list of all PlayerControl instances.
    /// </summary>
    internal static List<PlayerControl> AllPlayerControls = [];

    /// <summary>
    /// Gets the list of all alive PlayerControl instances.
    /// </summary>
    internal static List<PlayerControl> AllAlivePlayerControls => [.. AllPlayerControls.Where(pc => pc.IsAlive())];

    /// <summary>
    /// Gets all DeadBody objects in the scene.
    /// </summary>
    internal static DeadBody[] AllDeadBodys => [.. UnityEngine.Object.FindObjectsOfType<DeadBody>()];

    /// <summary>
    /// Gets all Vent objects in the scene.
    /// </summary>
    internal static Vent[] AllVents => UnityEngine.Object.FindObjectsOfType<Vent>();

    /// <summary>
    /// Gets the BepInEx logger instance.
    /// </summary>
    internal static ManualLogSource? Logger;

    public override void Load()
    {
        Instance = this;

        try
        {
            foreach (var listener in BepInEx.Logging.Logger.Listeners)
            {
                if (listener.GetType().Name.ToLower().Contains("Unity"))
                {
                    BepInEx.Logging.Logger.Listeners.Remove(listener);
                    break;
                }
            }

            if (!ModInfo.Starlight)
            {
                SetupConsole();
            }

            RegisterAllMonoBehavioursInAssembly();
            IL2CPPChainloader.Instance.Finished += OnChainloaderFinished;
        }
        catch (Exception ex)
        {
            Logger_.Error(ex);
        }
    }

    /// <summary>
    /// Runs when the BepInEx Chainloader has finished.
    /// </summary>
    private void OnChainloaderFinished()
    {
        if (!BAUModdedSupportEvents.InvokeAll_OnBAULoad(this)) return;

        BAUModdedSupportFlags.Initialize();
        GithubAPI.Connect();
        BAUConfigs.LoadConfigs();
        BetterDataManager.Initialize();
        Translator.Initialize();
        Harmony.PatchAll();
        GameSettingsPatch.SetupSettings(true);
        BAUModdedSupportEvents.InvokeAll_OnBAUOptionsLoaded([.. OptionItem.AllOptions.Cast<object>()]);
        InstanceAttribute.RegisterAll();
        OutfitData.Initialize();

        if (File.Exists(Path.Combine(BetterDataManager.filePathFolder, "better-log.txt")))
            File.WriteAllText(Path.Combine(BetterDataManager.filePathFolder, "better-previous-log.txt"), File.ReadAllText(Path.Combine(BetterDataManager.filePathFolder, "better-log.txt")));

        File.WriteAllText(Path.Combine(BetterDataManager.filePathFolder, "better-log.txt"), "");
        Logger_.Log("Better Among Us successfully loaded!");

        string SupportedVersions = string.Join(" ", ModInfo.SupportedAmongUsVersions);
        Logger_.Log($"BetterAmongUs {BetterAmongUsVersion}-{ModInfo.BuildDate} - [{AppVersion} --> {SupportedVersions}] {Utils.GetPlatformName(PlatformData.Platform)}");
    }

    /// <summary>
    /// Sets up the console window for logging.
    /// </summary>
    private static void SetupConsole()
    {
        ConsoleManager.CreateConsole();
        ConsoleManager.ConfigPreventClose.Value = true;
        if (ConsoleManager.ConfigConsoleEnabled.Value) ConsoleManager.DetachConsole();
        ConsoleManager.ConfigConsoleEnabled.Value = false;
        ConsoleManager.SetConsoleTitle("Among Us - BAU Console");
        Logger = BepInEx.Logging.Logger.CreateLogSource(ModInfo.PLUGIN_GUID);
        var customLogListener = new CustomLogListener();
        BepInEx.Logging.Logger.Listeners.Add(customLogListener);
        ConsoleManager.SetConsoleColor(ConsoleColor.Green);
        ConsoleManager.ConsoleStream.WriteLine($".--------------------------------------------------------------------------------.\r\n|  ____       _   _                 _                                  _   _     |\r\n| | __ )  ___| |_| |_ ___ _ __     / \\   _ __ ___   ___  _ __   __ _  | | | |___ |\r\n| |  _ \\ / _ \\ __| __/ _ \\ '__|   / _ \\ | '_ ` _ \\ / _ \\| '_ \\ / _` | | | | / __||\r\n| | |_) |  __/ |_| ||  __/ |     / ___ \\| | | | | | (_) | | | | (_| | | |_| \\__ \\|\r\n| |____/ \\___|\\__|\\__\\___|_|    /_/   \\_\\_| |_| |_|\\___/|_| |_|\\__, |  \\___/|___/|\r\n|                                                              |___/             |\r\n'--------------------------------------------------------------------------------'");
    }

    /// <summary>
    /// Registers all MonoBehaviour classes for IL2CPP injection.
    /// </summary>
    private static void RegisterAllMonoBehavioursInAssembly()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var monoBehaviourTypes = assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(MonoBehaviour)) && !type.IsAbstract)
            .OrderBy(type => type.Name);

        foreach (var type in monoBehaviourTypes)
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp(type);
            }
            catch (Exception ex)
            {
                Logger_.Error($"Failed to register MonoBehaviour: {type.FullName}\n{ex}");
            }
        }
    }

    /// <summary>
    /// Gets the persistent data path for Among Us.
    /// </summary>
    /// <returns>The persistent data path string.</returns>
    internal static string GetDataPathToAmongUs() => Application.persistentDataPath;

    /// <summary>
    /// Gets the game installation path for Among Us.
    /// </summary>
    /// <returns>The game installation path string.</returns>
    internal static string GetGamePathToAmongUs()
    {
        if (!ModInfo.Starlight)
        {
            return Path.GetDirectoryName(Application.dataPath) ?? throw new Exception("Unable to find `Application.dataPath` path");
        }
        else
        {
            return GetDataPathToAmongUs();
        }
    }
}