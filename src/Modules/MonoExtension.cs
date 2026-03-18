using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime;
using System.Collections;
using UnityEngine;

namespace BetterAmongUs.Modules;

/// <summary>
/// Interface for MonoBehavior extensions in BetterAmongUs.
/// </summary>
internal interface IMonoExtension
{
    /// <summary>
    /// Gets or sets the base MonoBehavior this extension is attached to.
    /// </summary>
    MonoBehaviour? BaseMono { get; set; }
}

/// <summary>
/// Generic interface for MonoBehavior extensions with specific base type.
/// </summary>
/// <typeparam name="T">The type of MonoBehavior this extension attaches to.</typeparam>
internal interface IMonoExtension<T> : IMonoExtension where T : MonoBehaviour
{
    /// <summary>
    /// Gets or sets the base MonoBehavior of type T.
    /// </summary>
    new T? BaseMono { get; set; }

    /// <summary>
    /// Explicit interface implementation for non-generic BaseMono.
    /// </summary>
    MonoBehaviour? IMonoExtension.BaseMono
    {
        get => BaseMono;
        set => BaseMono = value as T;
    }
}

/// <summary>
/// Manages MonoBehavior extensions and their lifecycle in BetterAmongUs.
/// </summary>
internal static class MonoExtensionManager
{
    private static readonly Dictionary<Type, List<ExtensionPair>> _extensionsByBaseType = [];

    /// <summary>
    /// Gets an extension of type T attached to a MonoBehavior.
    /// </summary>
    /// <typeparam name="T">The type of extension to retrieve.</typeparam>
    /// <param name="mono">The base MonoBehavior.</param>
    /// <returns>The extension instance, or null if not found.</returns>
    internal static T? Get<T>(MonoBehaviour mono) where T : class, IMonoExtension
    {
        if (mono == null) return null;

        if (_extensionsByBaseType.TryGetValue(mono.GetType(), out var extensions))
        {
            foreach (var pair in extensions)
            {
                if (pair.Base == mono && pair.Extension is T result)
                {
                    return result;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Runs a callback when a MonoBehavior extension becomes available.
    /// </summary>
    /// <typeparam name="T">The type of extension to wait for.</typeparam>
    /// <param name="mono">The base MonoBehavior.</param>
    /// <param name="getExtension">Function to retrieve the extension.</param>
    /// <param name="callback">Callback to execute when extension is available.</param>
    internal static void RunWhenNotNull<T>(MonoBehaviour mono, Func<T?> getExtension, Action<T> callback) where T : class, IMonoExtension
    {
        mono.StartCoroutine(CoWaitForExtension(getExtension, callback));
    }

    /// <summary>
    /// Coroutine that waits for an extension to become available.
    /// </summary>
    private static IEnumerator CoWaitForExtension<T>(Func<T?> getExtension, Action<T> callback) where T : class, IMonoExtension
    {
        T? extension;
        while ((extension = getExtension()) == null)
        {
            yield return null;
        }
        callback(extension);
    }

    /// <summary>
    /// Cleans up all registered extensions with null references.
    /// </summary>
    internal static void CleanAll()
    {
        foreach (var kvp in _extensionsByBaseType.ToArray())
        {
            var extensions = kvp.Value;
            for (int i = extensions.Count - 1; i >= 0; i--)
            {
                if (extensions[i].Base == null || extensions[i].Extension == null)
                {
                    extensions.RemoveAt(i);
                }
            }

            if (extensions.Count == 0)
            {
                _extensionsByBaseType.Remove(kvp.Key);
            }
        }
    }

    /// <summary>
    /// Registers a MonoBehavior extension with the manager.
    /// </summary>
    /// <param name="extension">The extension to register.</param>
    /// <returns>True if registration was successful, false otherwise.</returns>
    internal static bool RegisterExtension(this IMonoExtension extension)
    {
        // Try to find IMonoExtension<T> implementation
        Type? genericInterface = extension.GetType()
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMonoExtension<>));

        if (genericInterface == null)
        {
            if (extension is MonoBehaviour mono)
                UnityEngine.Object.Destroy(mono);
            return false;
        }

        // Get T from IMonoExtension<T>
        Type monoType = genericInterface.GetGenericArguments()[0];

        if (!monoType.IsAssignableTo(typeof(MonoBehaviour)))
        {
            if (extension is MonoBehaviour mono)
                UnityEngine.Object.Destroy(mono);
            return false;
        }

        var monoBehaviour = extension as MonoBehaviour;
        if (monoBehaviour == null)
            return false;

        // Get the base component (including inactive ones)
        var baseComponent = monoBehaviour.GetComponentInChildren(Il2CppType.From(monoType), true) as MonoBehaviour;
        if (baseComponent == null)
        {
            UnityEngine.Object.Destroy(monoBehaviour);
            return false;
        }

        // Check for duplicate extensions (including inactive ones)
        var existingExtensions = monoBehaviour.GetComponentsInChildren(Il2CppType.From(extension.GetType()), true);
        if (existingExtensions.Length > 1)
        {
            UnityEngine.Object.Destroy(monoBehaviour);
            return false;
        }

        // Register the extension
        if (!_extensionsByBaseType.TryGetValue(monoType, out var extensions))
        {
            extensions = [];
            _extensionsByBaseType[monoType] = extensions;
        }

        extensions.Add(new ExtensionPair { Base = baseComponent, Extension = extension });
        extension.BaseMono = baseComponent;

        return true;
    }

    /// <summary>
    /// Unregisters a MonoBehavior extension from the manager.
    /// </summary>
    /// <param name="extension">The extension to unregister.</param>
    internal static void UnregisterExtension(this IMonoExtension extension)
    {
        if (extension.BaseMono == null)
            return;

        if (_extensionsByBaseType.TryGetValue(extension.BaseMono.GetType(), out var extensions))
        {
            for (int i = extensions.Count - 1; i >= 0; i--)
            {
                if (extensions[i].Extension == extension)
                {
                    extensions.RemoveAt(i);
                    break;
                }
            }

            if (extensions.Count == 0)
            {
                _extensionsByBaseType.Remove(extension.BaseMono.GetType());
            }
        }
    }

    /// <summary>
    /// Represents a pairing between a base MonoBehavior and its extension.
    /// </summary>
    private struct ExtensionPair
    {
        /// <summary>
        /// The base MonoBehavior.
        /// </summary>
        internal MonoBehaviour? Base;

        /// <summary>
        /// The extension attached to the base.
        /// </summary>
        internal IMonoExtension? Extension;
    }
}