using BepInEx.Configuration;
using BetterAmongUs.Helpers;
using BetterAmongUs.Patches.Client;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterAmongUs.Modules;

/// <summary>
/// Represents a customizable client option item that can be toggled in the options menu.
/// </summary>
internal sealed class ClientOptionItem
{
    /// <summary>
    /// Gets the configuration entry associated with this option.
    /// </summary>
    internal ConfigEntry<bool>? Config { get; }

    /// <summary>
    /// Gets the toggle button behavior for this option.
    /// </summary>
    internal ToggleButtonBehaviour ToggleButton { get; }

    /// <summary>
    /// Gets the list of all created client option items.
    /// </summary>
    internal static readonly List<ClientOptionItem> ClientOptions = [];

    /// <summary>
    /// Creates a toggle option with configuration binding.
    /// </summary>
    /// <param name="name">The display name of the option.</param>
    /// <param name="config">The configuration entry to bind to.</param>
    /// <param name="optionsMenuBehaviour">The options menu behavior instance.</param>
    /// <param name="onToggle">Optional action to execute when the toggle is changed.</param>
    /// <param name="toggleCheck">Optional function to check if the toggle can be changed.</param>
    /// <returns>A new ClientOptionItem instance.</returns>
    public static ClientOptionItem CreateToggle(string name, ConfigEntry<bool> config, OptionsMenuBehaviour optionsMenuBehaviour, Action? onToggle = null, Func<bool>? toggleCheck = null)
    {
        var toggleButton = CreateToggleButton(name, config.Value, optionsMenuBehaviour);
        var item = new ClientOptionItem(name, config, toggleButton);

        item.SetupToggleButton(onToggle, toggleCheck);
        ClientOptions.Add(item);

        return item;
    }

    /// <summary>
    /// Creates a button option without toggle state.
    /// </summary>
    /// <param name="name">The display name of the button.</param>
    /// <param name="optionsMenuBehaviour">The options menu behavior instance.</param>
    /// <param name="onClick">The action to execute when the button is clicked.</param>
    /// <param name="clickCheck">Optional function to check if the button can be clicked.</param>
    /// <returns>A new ClientOptionItem instance.</returns>
    public static ClientOptionItem CreateButton(string name, OptionsMenuBehaviour optionsMenuBehaviour, Action onClick, Func<bool>? clickCheck = null)
    {
        var toggleButton = CreateToggleButton(name, false, optionsMenuBehaviour);
        var item = new ClientOptionItem(name, null, toggleButton);

        item.SetupButton(onClick, clickCheck);
        ClientOptions.Add(item);

        return item;
    }

    /// <summary>
    /// Creates a toggle option without configuration binding (manual state management).
    /// </summary>
    /// <param name="name">The display name of the option.</param>
    /// <param name="initialState">The initial state of the toggle.</param>
    /// <param name="optionsMenuBehaviour">The options menu behavior instance.</param>
    /// <param name="onToggle">The action to execute when the toggle is changed, receiving the new state.</param>
    /// <param name="toggleCheck">Optional function to check if the toggle can be changed.</param>
    /// <returns>A new ClientOptionItem instance.</returns>
    public static ClientOptionItem CreateManualToggle(string name, bool initialState, OptionsMenuBehaviour optionsMenuBehaviour, Action<bool> onToggle, Func<bool>? toggleCheck = null)
    {
        var toggleButton = CreateToggleButton(name, initialState, optionsMenuBehaviour);
        var item = new ClientOptionItem(name, null, toggleButton);

        item.SetupManualToggle(initialState, onToggle, toggleCheck);
        ClientOptions.Add(item);

        return item;
    }

    /// <summary>
    /// Initializes a new instance of the ClientOptionItem class.
    /// </summary>
    /// <param name="name">The name of the option.</param>
    /// <param name="config">The configuration entry, or null for non-config options.</param>
    /// <param name="toggleButton">The toggle button behavior instance.</param>
    private ClientOptionItem(string name, ConfigEntry<bool>? config, ToggleButtonBehaviour toggleButton)
    {
        Config = config;
        ToggleButton = toggleButton;
        ToggleButton.name = name;
    }

    /// <summary>
    /// Creates a toggle button GameObject for the options menu.
    /// </summary>
    /// <param name="name">The name of the toggle button.</param>
    /// <param name="initialState">The initial state of the toggle.</param>
    /// <param name="optionsMenuBehaviour">The options menu behavior instance.</param>
    /// <returns>A new ToggleButtonBehaviour instance.</returns>
    private static ToggleButtonBehaviour CreateToggleButton(string name, bool initialState, OptionsMenuBehaviour optionsMenuBehaviour)
    {
        if (OptionsMenuBehaviourPatch.BetterOptionsTab?.Content == null)
        {
            throw new InvalidOperationException("BetterOptionsTab is not initialized");
        }

        var mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;
        var toggleButton = Object.Instantiate(mouseMoveToggle, OptionsMenuBehaviourPatch.BetterOptionsTab.Content.transform);

        toggleButton.transform.localPosition = CalculateButtonPosition();
        toggleButton.name = name;
        toggleButton.Text.text = name;

        return toggleButton;
    }

    /// <summary>
    /// Calculates the position for a new button based on the current number of options.
    /// </summary>
    /// <returns>A Vector3 representing the button position.</returns>
    private static Vector3 CalculateButtonPosition()
    {
        return new Vector3(
            ClientOptions.Count % 2 == 0 ? -1.3f : 1.3f,
            1.8f - 0.5f * (ClientOptions.Count / 2),
            -6f
        );
    }

    /// <summary>
    /// Sets up a configuration-bound toggle button with click handler.
    /// </summary>
    /// <param name="onToggle">Optional action to execute when toggled.</param>
    /// <param name="toggleCheck">Optional function to check if the toggle can be changed.</param>
    private void SetupToggleButton(Action? onToggle, Func<bool>? toggleCheck)
    {
        var passiveButton = ToggleButton.GetComponent<PassiveButton>();
        passiveButton.OnClick = new();

        passiveButton.OnClick.AddListener(() =>
        {
            if (toggleCheck?.Invoke() == false) return;

            if (Config != null)
            {
                Config.Value = !Config.Value;
                UpdateToggle();
            }
            onToggle?.Invoke();
        });

        UpdateToggle();
    }

    /// <summary>
    /// Sets up a button (non-toggle) with click handler.
    /// </summary>
    /// <param name="onClick">The action to execute when clicked.</param>
    /// <param name="clickCheck">Optional function to check if the button can be clicked.</param>
    private void SetupButton(Action onClick, Func<bool>? clickCheck)
    {
        var passiveButton = ToggleButton.GetComponent<PassiveButton>();
        passiveButton.OnClick = new();

        // Style for button (not toggle)
        ToggleButton.Text.text = ToggleButton.name;
        ToggleButton.Rollover?.ChangeOutColor(new Color32(0, 150, 0, 255));
        ToggleButton.Text.color = new Color(1f, 1f, 1f, 1f);

        passiveButton.OnClick.AddListener(() =>
        {
            if (clickCheck?.Invoke() == false) return;
            onClick?.Invoke();
        });
    }

    /// <summary>
    /// Sets up a manually managed toggle button with state tracking.
    /// </summary>
    /// <param name="initialState">The initial state of the toggle.</param>
    /// <param name="onToggle">The action to execute when toggled, receiving the new state.</param>
    /// <param name="toggleCheck">Optional function to check if the toggle can be changed.</param>
    private void SetupManualToggle(bool initialState, Action<bool> onToggle, Func<bool>? toggleCheck)
    {
        var passiveButton = ToggleButton.GetComponent<PassiveButton>();
        passiveButton.OnClick = new();

        bool currentState = initialState;

        passiveButton.OnClick.AddListener(() =>
        {
            if (toggleCheck?.Invoke() == false) return;

            currentState = !currentState;
            UpdateManualToggle(currentState);
            onToggle?.Invoke(currentState);
        });

        UpdateManualToggle(initialState);
    }

    /// <summary>
    /// Updates the visual state of a config-bound toggle button.
    /// </summary>
    internal void UpdateToggle()
    {
        if (ToggleButton == null || Config == null) return;

        UpdateToggleVisuals(Config.Value);
    }

    /// <summary>
    /// Updates the visual state of a manually managed toggle button.
    /// </summary>
    /// <param name="isEnabled">The current enabled state of the toggle.</param>
    internal void UpdateManualToggle(bool isEnabled)
    {
        if (ToggleButton == null) return;

        UpdateToggleVisuals(isEnabled);
    }

    /// <summary>
    /// Updates the visual appearance of a toggle button based on its state.
    /// </summary>
    /// <param name="isEnabled">Whether the toggle is enabled.</param>
    private void UpdateToggleVisuals(bool isEnabled)
    {
        var color = isEnabled ?
            new Color32(0, 150, 0, 255) :
            new Color32(77, 77, 77, 255);

        var textColor = isEnabled ?
            new Color(1f, 1f, 1f, 1f) :
            new Color(1f, 1f, 1f, 0.5f);

        ToggleButton.Background.color = color;
        ToggleButton.Rollover?.ChangeOutColor(color);
        ToggleButton.Text.color = textColor;
        ToggleButton.Text.text = $"{ToggleButton.name}: {(isEnabled ? "On" : "Off")}";
    }
}