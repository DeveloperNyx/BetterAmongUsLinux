using BepInEx.Unity.IL2CPP;

namespace BetterAmongUs.Examples;

/// <summary>
/// Example plugin demonstrating how to integrate with BetterAmongUs modded support system.
/// Shows the structure required for a plugin to declare BAU events and flags.
/// </summary>
/// <remarks>
/// This example shows a minimal plugin that implements both event handlers and flags.
/// In a real implementation, you would add your plugin's actual logic in the Load() method.
/// </remarks>
internal class ModdedSupportPluginExample : BasePlugin
{
    /// <summary>
    /// Event handler instance for BetterAmongUs events.
    /// This field must be named exactly "BAUEvents" to be detected by the reflection system.
    /// </summary>
    public static ModdedSupportBAUEventExample BAUEvents = new();

    /// <summary>
    /// Array of flags to control BetterAmongUs behavior.
    /// This field must be named exactly "BAUFlags" to be detected by the reflection system.
    /// </summary>
    /// <remarks>
    /// Flags can disable specific BAU features or modify its behavior.
    /// Check Modules.Support.BAUModdedSupportFlags for available flag constants.
    /// </remarks>
    public static string[] BAUFlags = [];

    /// <summary>
    /// Main plugin load method.
    /// In a real implementation, this is where your plugin initialization would occur.
    /// </summary>
    public override void Load()
    {
        // Plugin initialization logic would go here
    }
}