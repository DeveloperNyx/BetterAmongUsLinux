#pragma warning disable CA2211

using BepInEx;
using BepInEx.Unity.IL2CPP;
using BetterAmongUs.Helpers;
using System.Reflection;

namespace BetterAmongUs.Modules.Support;

/// <summary>
/// Provides modded support functionality for BetterAmongUs by allowing other mods to declare flags
/// that control various features and behaviors of BetterAmongUs.
/// </summary>
public static class BAUModdedSupportFlags
{
    // ============================================
    // Client Features - General
    // ============================================

    /// <summary>
    /// Flag to disable the anticheat system. When set by another mod, BetterAmongUs will disable its anticheat features.
    /// </summary>
    public static string Disable_Anticheat = "client.disable.anticheat";

    /// <summary>
    /// Flag to disable the Better Ping Tracker feature. When set by another mod, BetterAmongUs will not patch out the normal ping tracker.
    /// </summary>
    public static string Disable_BetterPingTracker = "client.disable.betterpingtracker";

    /// <summary>
    /// Flag to disable the Private Lobby feature. When set by another mod, BetterAmongUs will disable private only lobbies.
    /// </summary>
    public static string Disable_PrivateLobby = "client.disable.privatelobby";

    /// <summary>
    /// Flag to disable theme. When set by another mod, BetterAmongUs will disable its theming features.
    /// </summary>
    public static string Disable_Theme = "client.disable.theme";

    /// <summary>
    /// Flag to disable custom mod stamp sprite. When set by another mod, BetterAmongUs will disable its custom mod stamp sprite.
    /// </summary>
    public static string Disable_CustomModStamp = "client.disable.custommodstamp";

    // ============================================
    // Client Features - Command System
    // ============================================

    /// <summary>
    /// Flag to disable command system. When set by another mod, BetterAmongUs will disable its command features.
    /// </summary>
    public static string Disable_AllCommands = "command.disable.allcommands";

    /// <summary>
    /// Flag prefix to disable specific commands. The full flag should be "client.disable=COMMAND_NAME".
    /// When set by another mod, BetterAmongUs will disable the specified command.
    /// </summary>
    public static string Disable_Command = "command.disable=";

    /// <summary>
    /// Flag prefix for forcing the command prefix for BAU to be "bau:"
    /// </summary>
    public static string Force_BAU_Command_Prefix = "command.force.bau.prefix";

    // ============================================
    // Client Features - Game Settings
    // ============================================

    /// <summary>
    /// Flag to disable game settings modifications. When set by another mod, BetterAmongUs will not modify game settings.
    /// </summary>
    public static string Disable_AllGameSettings = "gamesetting.disable.allgamesettings";

    /// <summary>
    /// Flag prefix to disable specific game setting. The full flag should be "client.disable=TRANSLATION_NAME".
    /// When set by another mod, BetterAmongUs will disable the specified game setting and uses default value.
    /// </summary>
    public static string Disable_GameSetting = "gamesetting.disable=";

    // ============================================
    // Lobby Features
    // ============================================

    /// <summary>
    /// Flag to disable cancel starting game. When set by another mod, BetterAmongUs will not patch start game button.
    /// </summary>
    public static string Disable_CancelStartingGame = "lobby.disable.cancelstartinggame";

    // ============================================
    // Main Menu Features
    // ============================================

    /// <summary>
    /// Flag to disable mod update button. When set by another mod, BetterAmongUs will not show update button in the main menu.
    /// </summary>
    public static string Disable_ModUpdate = "mainmenu.disable.modupdate";

    /// <summary>
    /// Flag to disable the BetterAmongUs logo in the main menu. When set by another mod, BetterAmongUs will hide its logo.
    /// </summary>
    public static string Disable_BAULogo = "mainmenu.disable.baulogo";

    private static readonly HashSet<string> _flags = [];
    private static bool _initialized = false;

    /// <summary>
    /// Initializes the modded support system.
    /// </summary>
    internal static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        foreach (var pluginInfo in IL2CPPChainloader.Instance.Plugins.Values)
        {
            TryGetFlags((BasePlugin)pluginInfo.Instance);
        }
    }

    /// <summary>
    /// Attempts to extract BAUFlags from a loaded plugin's fields.
    /// </summary>
    /// <param name="plugin">The plugin instance.</param>
    private static void TryGetFlags(BasePlugin plugin)
    {
        var field = plugin.GetType().GetField("BAUFlags",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        if (field == null) return;

        var value = field.IsStatic ? field.GetValue(null) : field.GetValue(plugin);

        if (value is not IEnumerable<string> strings) return;

        var pluginName = plugin.GetType().GetCustomAttribute<BepInPlugin>()?.Name ?? plugin.GetType().Name;

        foreach (var flag in strings)
        {
            if (_flags.Add(flag))
            {
                Logger_.Log($"Loaded '{flag}' flag from {pluginName}", "BAUModdedSupport");
            }
        }
    }

    /// <summary>
    /// Manually adds a flag to the internal flag collection.
    /// </summary>
    /// <param name="flag">The flag string to add to the collection.</param>
    internal static void AddFlag(string flag)
    {
        _flags.Add(flag);
    }

    /// <summary>
    /// Checks if a specific flag has been declared by any loaded mod.
    /// </summary>
    /// <param name="flag">The flag to check for presence in the collected flags.</param>
    /// <returns>True if the flag is present, false otherwise.</returns>
    public static bool HasFlag(string flag)
    {
        return _flags.Contains(flag);
    }
}