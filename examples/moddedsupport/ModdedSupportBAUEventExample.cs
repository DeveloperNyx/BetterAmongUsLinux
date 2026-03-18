using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;

namespace BetterAmongUs.Examples;

/// <summary>
/// Example event handler class demonstrating how to respond to BetterAmongUs events.
/// This class must be instantiated and assigned to the BAUEvents field in the plugin.
/// </summary>
/// <remarks>
/// Each method in this class corresponds to a different lifecycle event in BetterAmongUs.
/// Methods are optional - only implement the ones your plugin needs.
/// </remarks>
internal class ModdedSupportBAUEventExample
{
    /// <summary>
    /// Called when BetterAmongUs is loading. Can be used to prevent BAU from loading.
    /// </summary>
    /// <param name="bauPlugin">The BetterAmongUs plugin instance.</param>
    /// <returns>
    /// Return true to allow BAU to continue loading, or false to prevent BAU from loading.
    /// This can be useful if your plugin has compatibility issues with specific BAU versions.
    /// </returns>
    public bool OnBAULoad(BasePlugin bauPlugin)
    {
        return true;
    }

    /// <summary>
    /// Called when BetterAmongUs game options have been loaded.
    /// Allows plugins to read or modify BAU game options.
    /// </summary>
    /// <param name="options">
    /// Array of game options objects from BetterAmongUs.
    /// The exact type of these objects depends on BAU's internal implementation.
    /// Common option types to check for:
    /// Modules.OptionItems.OptionItem,
    /// Modules.OptionItems.OptionItem{T}, 
    /// Modules.OptionItems.OptionCheckboxItem,
    /// Modules.OptionItems.OptionFloatItem,
    /// Modules.OptionItems.OptionIntItem,
    /// Modules.OptionItems.OptionPercentItem,
    /// Modules.OptionItems.OptionPlayerItem,
    /// Modules.OptionItems.OptionStringItem,
    /// </param>
    public void OnBAUOptionsLoaded(object[] options)
    {
    }

    /// <summary>
    /// Called when BetterAmongUs configuration entries have been loaded.
    /// Allows plugins to read or modify BAU's BepInEx configuration entries.
    /// </summary>
    /// <param name="configs">
    /// Array of BepInEx configuration entries from BetterAmongUs.
    /// These can be cast to ConfigEntry&lt;T&gt; to access their values.
    /// </param>
    /// <remarks>
    /// This is useful for plugins that need to interact with BAU's configuration,
    /// such as reading default values or adding validation to certain settings.
    /// </remarks>
    public void OnBAUConfigEntriesLoaded(ConfigEntryBase[] configs)
    {
    }
}