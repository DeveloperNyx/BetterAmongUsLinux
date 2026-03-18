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
    // Client Features
    // ============================================

    /// <summary>
    /// Disables the enhanced ping display
    /// When enabled by another mod, BetterAmongUs will not replace the default ping tracker.
    /// </summary>
    public static string Disable_BetterPingTracker = "client.disable.betterpingtracker";

    /// <summary>
    /// Disables private lobby functionality.
    /// When enabled by another mod, BetterAmongUs will remove private only option when creating a lobby.
    /// </summary>
    public static string Disable_PrivateLobby = "client.disable.privatelobby";

    /// <summary>
    /// Disables all theming and customization features.
    /// When enabled by another mod, BetterAmongUs will use the default game appearance.
    /// </summary>
    public static string Disable_Theme = "client.disable.theme";

    /// <summary>
    /// Disables the custom mod badge/stamp.
    /// When enabled by another mod, BetterAmongUs will use the default mod indicator.
    /// </summary>
    public static string Disable_CustomModStamp = "client.disable.custommodstamp";

    /// <summary>
    /// Disables the custom server region dropdown menu.
    /// When enabled by another mod, BetterAmongUs will use the default server selection interface.
    /// </summary>
    public static string Disable_ServerDropDown = "client.disable.serverdropdown";

    /// <summary>
    /// Disables Discord Rich Presence integration.
    /// When enabled by another mod, BetterAmongUs will not update Discord status.
    /// </summary>
    public static string Disable_DiscordRP = "client.disable.discordrp";

    /// <summary>
    /// Disables the custom HTTP header that identifies BetterAmongUs clients to matchmaking servers.
    /// When enabled by another mod, BetterAmongUs will use standard HTTP headers.
    /// </summary>
    public static string Disable_BAUHttpHeader = "client.disable.bauhttpheader";

    // ============================================
    // Anti-Cheat System
    // ============================================

    /// <summary>
    /// Completely disables the anti-cheat system.
    /// When enabled by another mod, all anti-cheat features will be inactive.
    /// </summary>
    public static string Disable_Anticheat = "anticheat.disable";

    /// <summary>
    /// Prefix for disabling specific RPC handlers or handler flags.
    /// Format: "anticheat.disable.rpchandler=HandlerClassName" to disable entire handler,
    /// or "anticheat.disable.rpchandler=HandlerClassName:HandlerFlagName" for specific flags.
    /// When enabled by another mod, BetterAmongUs will skip the specified RPC validations.
    /// <seealso cref="AntiCheat.RPCHandler"/> for base handler class.
    /// <seealso cref="Enums.HandlerFlag"/> for available handler flags.
    /// </summary>
    public static string Disable_RPCHandler = "anticheat.disable.rpchandler=";

    // ============================================
    // Command System
    // ============================================

    /// <summary>
    /// Disables the entire command system.
    /// When enabled by another mod, no BetterAmongUs commands will be available.
    /// </summary>
    public static string Disable_AllCommands = "command.disable.allcommands";

    /// <summary>
    /// Prefix for disabling specific commands.
    /// Format: "command.disable=COMMAND_NAME"
    /// When enabled by another mod, the specified command will be unavailable.
    /// <seealso cref="Commands.BaseCommand"/> for base command class.
    /// </summary>
    public static string Disable_Command = "command.disable=";

    /// <summary>
    /// Forces all BetterAmongUs commands to use the "bau:" prefix.
    /// When enabled by another mod, commands must be prefixed with "bau:" to work.
    /// </summary>
    public static string Force_BAU_Command_Prefix = "command.force.bau.prefix";

    // ============================================
    // Game Options & Settings
    // ============================================

    /// <summary>
    /// Disables all custom game option modifications.
    /// When enabled by another mod, BetterAmongUs will use default game options.
    /// </summary>
    public static string Disable_AllGameOptions = "gameoption.disable.allgameoptions";

    /// <summary>
    /// Prefix for disabling specific game options.
    /// Format: "gameoption.disable=TRANSLATION_NAME"
    /// When enabled by another mod, the specified option will be hidden and use default values.
    /// <seealso cref="OptionItems.OptionItem"/> for base option class.
    /// </summary>
    public static string Disable_GameOption = "gameoption.disable=";

    // ============================================
    // Lobby Features
    // ============================================

    /// <summary>
    /// Disables the ability to cancel game start countdown.
    /// When enabled by another mod, the start game button cannot be interrupted once clicked.
    /// </summary>
    public static string Disable_CancelStartingGame = "lobby.disable.cancelstartinggame";

    // ============================================
    // Main Menu Features
    // ============================================

    /// <summary>
    /// Disables the mod update notification button.
    /// When enabled by another mod, update checks and prompts will not appear in the main menu.
    /// </summary>
    public static string Disable_ModUpdate = "mainmenu.disable.modupdate";

    /// <summary>
    /// Disables the BetterAmongUs logo/branding in the main menu.
    /// When enabled by another mod, the BAU logo will be hidden.
    /// </summary>
    public static string Disable_BAULogo = "mainmenu.disable.baulogo";

    // ============================================
    // Gameplay Features
    // ============================================

    /// <summary>
    /// Disables the enhanced role assignment algorithm.
    /// When enabled by another mod, BetterAmongUs will use the default role distribution system.
    /// </summary>
    public static string Disable_BetterRoleAlgorithm = "gameplay.disable.betterrolealgorithm";

    /// <summary>
    /// Disables the detailed end-game summary screen.
    /// When enabled by another mod, only the default end-game screen will be shown.
    /// </summary>
    public static string Disable_EndGameSummary = "gameplay.disable.endgamesummary";

    /// <summary>
    /// Disables custom icon on mini map.
    /// When enabled by another mod, custom icons on the mini map will not be generated.
    /// </summary>
    public static string Disable_MinimapIcons = "gameplay.disable.minimapicons";

    /// <summary>
    /// Disables custom vent color highlights.
    /// When enabled by another mod, vent colors when highlighted will not use vent groups.
    /// </summary>
    public static string Disable_VentColorGroups = "gameplay.disable.ventcolorgroups";

    private static readonly HashSet<string> _flags = [];
    private static bool _initialized = false;

    /// <summary>
    /// Initializes the modded support system.
    /// </summary>
    internal static void Initialize()
    {
        if (_initialized)
            return;

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

        if (field == null)
            return;

        var value = field.IsStatic ? field.GetValue(null) : field.GetValue(plugin);

        if (value is not IEnumerable<string> strings)
            return;

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