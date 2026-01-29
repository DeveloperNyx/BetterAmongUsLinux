using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using System.Reflection;

namespace BetterAmongUs.Modules.Support;

/// <summary>
/// Provides a system for modded plugins to interact with BetterAmongUs through reflection-based events.
/// Allows other plugins to define event handlers without direct assembly references.
/// </summary>
/// <remarks>
/// Plugins can implement event handlers by creating a class with specific method signatures.
/// The system automatically discovers and invokes these handlers at runtime.
/// </remarks>
public class BAUModdedSupportEvents
{
    // ============================================
    // Method Structures
    // ============================================

    /// <summary>
    /// Default implementation of the OnBAULoad event handler.
    /// </summary>
    /// <param name="bauPlugin">The BetterAmongUs plugin instance.</param>
    /// <returns>Always returns true to indicate successful loading.</returns>
    public bool OnBAULoad(BasePlugin bauPlugin) => true;

    /// <summary>
    /// Default implementation of the OnBAUSettingsLoaded event handler.
    /// </summary>
    /// <param name="settings">The loaded settings from BetterAmongUs.</param>
    public void OnBAUSettingsLoaded(object[] settings) { }

    /// <summary>
    /// Default implementation of the OnBAUConfigEntriesLoaded event handler.
    /// </summary>
    /// <param name="configs">An array of BepInEx configuration entries from BetterAmongUs.</param>
    public void OnBAUConfigEntriesLoaded(ConfigEntryBase[] configs) { }

    // ============================================
    // Method Structures
    // ============================================

    /// <summary>
    /// Invokes the OnBAULoad event handler for all loaded plugins.
    /// </summary>
    /// <param name="bauPlugin">The BetterAmongUs plugin instance.</param>
    /// <returns>
    /// Returns false if any plugin's OnBAULoad handler returns false, otherwise returns true.
    /// Returns true if no plugins implement the handler.
    /// </returns>
    internal static bool InvokeAll_OnBAULoad(BasePlugin bauPlugin)
    {
        foreach (var pluginInfo in IL2CPPChainloader.Instance.Plugins.Values)
        {
            var plugin = (BasePlugin)pluginInfo.Instance;
            if (InvokePluginMethod<bool?>(plugin, nameof(OnBAULoad), defaultValue: true, bauPlugin) == false)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Invokes the OnBAUSettingsLoaded event handler for all loaded plugins.
    /// </summary>
    /// <param name="settings">The loaded settings from BetterAmongUs.</param>
    internal static void InvokeAll_OnBAUSettingsLoaded(object[] settings)
    {
        foreach (var pluginInfo in IL2CPPChainloader.Instance.Plugins.Values)
        {
            var plugin = (BasePlugin)pluginInfo.Instance;
            InvokePluginMethod<object>(plugin, nameof(OnBAUSettingsLoaded), parameters: settings);
        }
    }

    /// <summary>
    /// Invokes the OnBAUConfigEntriesLoaded event handler for all loaded plugins.
    /// </summary>
    /// <param name="configs">An array of BepInEx configuration entries from BetterAmongUs.</param>
    internal static void InvokeAll_OnBAUConfigEntriesLoaded(ConfigEntryBase[] configs)
    {
        foreach (var pluginInfo in IL2CPPChainloader.Instance.Plugins.Values)
        {
            var plugin = (BasePlugin)pluginInfo.Instance;
            InvokePluginMethod<object>(plugin, nameof(OnBAUConfigEntriesLoaded), parameters: configs);
        }
    }

    /// <summary>
    /// Invokes a specific method on a plugin's event class using reflection.
    /// </summary>
    /// <typeparam name="T">The expected return type of the method.</typeparam>
    /// <param name="plugin">The plugin instance to invoke the method on.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="defaultValue">The value to return if the method cannot be invoked.</param>
    /// <param name="parameters">The parameters to pass to the method.</param>
    /// <returns>
    /// The result of the method invocation, or <paramref name="defaultValue"/> if the method
    /// cannot be found, invocation fails, or the return type doesn't match <typeparamref name="T"/>.
    /// </returns>
    private static T? InvokePluginMethod<T>(BasePlugin plugin, string methodName, T? defaultValue = default, params object[] parameters)
    {
        var eventClass = GetEventClass(plugin);
        if (eventClass == null) return defaultValue;

        var method = eventClass.GetType().GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null) return defaultValue;

        try
        {
            var result = method.Invoke(eventClass, parameters);

            return result is T typedResult ? typedResult : defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Retrieves the event class instance from a plugin using reflection.
    /// </summary>
    /// <param name="plugin">The plugin instance to search for the event class.</param>
    /// <returns>
    /// The event class instance if found, otherwise null.
    /// </returns>
    private static object? GetEventClass(BasePlugin plugin)
    {
        const string EventFieldName = "BAUEvents";

        var field = plugin.GetType().GetField(EventFieldName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        if (field == null) return null;

        var value = field.IsStatic
            ? field.GetValue(null)
            : field.GetValue(plugin);

        return value?.GetType().IsClass == true ? value : null;
    }
}