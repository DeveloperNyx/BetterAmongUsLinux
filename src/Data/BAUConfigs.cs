using BepInEx.Configuration;
using BetterAmongUs.Modules.Support;
using BetterAmongUs.Patches.Client;

namespace BetterAmongUs.Data;

/// <summary>
/// Manages configuration entries for the Better Among Us.
/// </summary>
internal static class BAUConfigs
{
    /// <summary>
    /// Gets or sets the configuration entry for private only lobby setting.
    /// </summary>
    internal static ConfigEntry<bool>? PrivateOnlyLobby { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for anti-cheat setting.
    /// </summary>
    internal static ConfigEntry<bool>? AntiCheat { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for sending Better RPC setting.
    /// </summary>
    internal static ConfigEntry<bool>? SendBetterRpc { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for better notifications setting.
    /// </summary>
    internal static ConfigEntry<bool>? BetterNotifications { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for force own language setting.
    /// </summary>
    internal static ConfigEntry<bool>? ForceOwnLanguage { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for chat dark mode setting.
    /// </summary>
    internal static ConfigEntry<bool>? ChatDarkMode { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for chat in gameplay setting.
    /// </summary>
    internal static ConfigEntry<bool>? ChatInGameplay { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for lobby player info setting.
    /// </summary>
    internal static ConfigEntry<bool>? LobbyPlayerInfo { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for disable lobby theme setting.
    /// </summary>
    internal static ConfigEntry<bool>? DisableLobbyTheme { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for unlock FPS setting.
    /// </summary>
    internal static ConfigEntry<bool>? UnlockFPS { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for show FPS setting.
    /// </summary>
    internal static ConfigEntry<bool>? ShowFPS { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for command prefix setting.
    /// </summary>
    internal static ConfigEntry<string>? CommandPrefix { get; set; }

    /// <summary>
    /// Gets or sets the configuration entry for favorite color setting.
    /// </summary>
    internal static ConfigEntry<int>? FavoriteColor { get; set; }

    /// <summary>
    /// Gets or sets the configuration entry for the settings preset.
    /// </summary>
    internal static ConfigEntry<int>? SettingsPreset { get; private set; }

    /// <summary>
    /// Loads configuration options from BepInEx config file.
    /// </summary>
    internal static void LoadConfigs(BAUPlugin plugin)
    {
        PrivateOnlyLobby = plugin.Config.Bind("Mod", "PrivateOnlyLobby", false);
        AntiCheat = plugin.Config.Bind("Better Options", "AntiCheat", true);
        SendBetterRpc = plugin.Config.Bind("Better Options", "SendBetterRpc", true);
        BetterNotifications = plugin.Config.Bind("Better Options", "BetterNotifications", true);
        ForceOwnLanguage = plugin.Config.Bind("Better Options", "ForceOwnLanguage", false);
        ChatDarkMode = plugin.Config.Bind("Better Options", "ChatDarkMode", true);
        ChatInGameplay = plugin.Config.Bind("Better Options", "ChatInGameplay", true);
        LobbyPlayerInfo = plugin.Config.Bind("Better Options", "LobbyPlayerInfo", true);
        DisableLobbyTheme = plugin.Config.Bind("Better Options", "DisableLobbyTheme", true);
        UnlockFPS = plugin.Config.Bind("Better Options", "UnlockFPS", false);
        ShowFPS = plugin.Config.Bind("Better Options", "ShowFPS", false);
        CommandPrefix = plugin.Config.Bind("Client Options", "CommandPrefix", "/");
        FavoriteColor = plugin.Config.Bind("Mod", "FavoriteColor", -1);
        SettingsPreset = plugin.Config.Bind("Mod", "SettingsPreset", 0);

        BAUModdedSupportEvents.InvokeAll_OnBAUConfigEntriesLoaded([
            PrivateOnlyLobby, AntiCheat, SendBetterRpc,
            BetterNotifications, ForceOwnLanguage, ChatDarkMode,
            ChatInGameplay, LobbyPlayerInfo, DisableLobbyTheme,
            UnlockFPS, ShowFPS, CommandPrefix,
            FavoriteColor, SettingsPreset
        ]);

        OptionsMenuBehaviourPatch.UpdateFrameRate();
    }
}
