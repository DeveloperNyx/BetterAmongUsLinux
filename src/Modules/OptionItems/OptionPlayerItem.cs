using BetterAmongUs.Helpers;
using UnityEngine;

namespace BetterAmongUs.Modules.OptionItems;

/// <summary>
/// Represents an option item that selects a player from the current game.
/// </summary>
public sealed class OptionPlayerItem : OptionItem<int>
{
    /// <summary>
    /// Gets whether child options should be shown, based on whether a valid player is selected.
    /// </summary>
    internal sealed override bool ShowChildren => base.ShowChildren && Value > Min;

    /// <summary>
    /// Gets the maximum player index based on the number of players in the game.
    /// </summary>
    private int Max => BAUPlugin.AllPlayerControls.Count - 1;

    /// <summary>
    /// Gets the minimum player index (-1 for random selection, 0 for first player).
    /// </summary>
    private int Min => CanBeRandom ? -1 : 0;

    /// <summary>
    /// Gets whether this option allows random player selection.
    /// </summary>
    private bool CanBeRandom { get; set; }

    /// <summary>
    /// Gets whether this option can load values from persistent storage.
    /// </summary>
    internal override bool CanLoad => false;

    private static List<OptionPlayerItem> optionPlayerItems = [];

    /// <summary>
    /// Creates a new player option item or returns an existing one with the same ID.
    /// </summary>
    /// <param name="id">The unique identifier for this option.</param>
    /// <param name="tab">The tab this option belongs to.</param>
    /// <param name="tranStr">The translation key for the option name.</param>
    /// <param name="parent">Optional parent option for hierarchical organization.</param>
    /// <param name="canBeRandom">Whether this option allows random player selection.</param>
    /// <returns>A new or existing OptionPlayerItem instance.</returns>
    internal static OptionPlayerItem Create(int id, OptionTab tab, string tranStr, OptionItem? parent = null, bool canBeRandom = true)
    {
        if (optionPlayerItems.FirstOrDefault(opt => opt.Id == id) is OptionPlayerItem playerItem)
        {
            playerItem.CreateBehavior();
            return playerItem;
        }

        OptionPlayerItem Item = new();
        optionPlayerItems.Add(Item);
        Item.Value = canBeRandom ? -1 : 0; ;
        Item._id = id;
        Item.Tab = tab;
        Item.Translation = tranStr;
        Item.CanBeRandom = canBeRandom;

        if (parent != null)
        {
            Item.Parent = parent;
            parent.Children.Add(Item);
        }

        Item.CreateBehavior();
        return Item;
    }

    /// <summary>
    /// Player options are not saved to persistent storage.
    /// </summary>
    internal sealed override void Save()
    {
    }

    /// <summary>
    /// Creates the UI behavior for this player option.
    /// </summary>
    protected sealed override void CreateBehavior()
    {
        if (!GameSettingMenu.Instance)
            return;

        AllOptionsTemp.Add(this);
        var numberOption = UnityEngine.Object.Instantiate(Tab.AUTab.numberOptionOrigin, Tab.AUTab.settingsContainer);
        Option = numberOption;
        Obj = Option.gameObject;
        Option.enabled = false;
        Tab.Children.Add(this);
        TitleTMP = numberOption.TitleText;
        ValueTMP = numberOption.ValueText;
        SetupText(numberOption.TitleText);
        SetupOptionBehavior();
        SetOptionVisuals();
    }

    /// <summary>
    /// Sets up the specific behavior for the NumberOption component.
    /// </summary>
    protected sealed override void SetupOptionBehavior()
    {
        if (Option is NumberOption numberOption)
        {
            SetupAUOption(Option);
            numberOption.DestroyTextTranslators();
            numberOption.TitleText.text = Name;
            numberOption.PlusBtn.OnClick = new();
            numberOption.PlusBtn.OnClick.AddListener(Increase);
            numberOption.MinusBtn.OnClick = new();
            numberOption.MinusBtn.OnClick.AddListener(Decrease);
        }
    }

    /// <summary>
    /// Increases the player selection index based on modifier keys.
    /// </summary>
    private void Increase()
    {
        int plus = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            plus = 5;
        if (Input.GetKey(KeyCode.LeftControl))
            plus = 10;
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftControl))
            plus = 25;
        var value = Value;
        value += 1 * plus;
        SetValue(value);
    }

    /// <summary>
    /// Decreases the player selection index based on modifier keys.
    /// </summary>
    private void Decrease()
    {
        int plus = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            plus = 5;
        if (Input.GetKey(KeyCode.LeftControl))
            plus = 10;
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftControl))
            plus = 25;
        var value = Value;
        value -= 1 * plus;
        SetValue(value);
    }

    /// <summary>
    /// Sets the player selection value, clamping it to the valid range.
    /// </summary>
    /// <param name="newValue">The new player index to set.</param>
    public sealed override void SetValue(int newValue)
    {
        newValue = Math.Clamp(newValue, Min, Max);
        base.SetValue(newValue);
    }

    /// <summary>
    /// Resets all player option items to their minimum values.
    /// </summary>
    internal static void ResetAllValues()
    {
        foreach (var opt in optionPlayerItems)
        {
            opt.ResetValue();
        }
    }

    /// <summary>
    /// Updates all player option items to ensure their values are within valid ranges.
    /// </summary>
    internal static void UpdateAllValues()
    {
        foreach (var opt in optionPlayerItems)
        {
            opt.UpdateValue();
        }
    }

    /// <summary>
    /// Updates this player option's value to ensure it's within the valid range.
    /// </summary>
    internal void UpdateValue()
    {
        Value = Math.Clamp(Value, Min, Max);
        UpdateVisuals();
    }

    /// <summary>
    /// Resets this player option to its minimum value.
    /// </summary>
    internal void ResetValue()
    {
        Value = Min;
    }

    /// <summary>
    /// Updates the visual appearance of the player option based on its current value.
    /// </summary>
    /// <param name="updateTabVisuals">Whether to update the parent tab visuals as well.</param>
    internal sealed override void UpdateVisuals(bool updateTabVisuals = true)
    {
        if (!GameSettingMenu.Instance)
            return;

        if (Option is NumberOption numberOption)
        {
            numberOption.PlusBtn.SetInteractable(false);
            numberOption.MinusBtn.SetInteractable(false);

            if (Value < Max)
            {
                numberOption.PlusBtn.SetInteractable(true);
            }
            if (Value > Min)
            {
                numberOption.MinusBtn.SetInteractable(true);
            }

            numberOption.ValueText.text = ValueAsString().Replace(InfiniteIcon, InfiniteIcon.Size(200f));
        }

        if (updateTabVisuals)
        {
            Tab.UpdateVisuals();
        }
    }

    /// <summary>
    /// Gets the string representation of the selected player.
    /// </summary>
    /// <returns>The player's colored name or "Random" for random selection.</returns>
    public sealed override string ValueAsString()
    {
        if (Value != -1)
        {
            var player = Utils.PlayerFromPlayerId(Value);
            if (player != null)
                return $"{player.GetPlayerNameAndColor()}";
            else
                return "???";
        }
        else
        {
            return Translator.GetString(StringNames.RoundRobin).ToColor(Color.gray);
        }
    }

    /// <summary>
    /// Gets the integer value (player index) of this option.
    /// </summary>
    /// <returns>The current player index.</returns>
    public sealed override int GetInt() =>
        GetValue();

    /// <summary>
    /// Gets the float representation of the player index.
    /// </summary>
    /// <returns>The current player index as a float.</returns>
    public sealed override float GetFloat() =>
        GetValue();

    /// <summary>
    /// Checks if the option value matches a specific integer.
    /// </summary>
    /// <param name="@int">The integer value to compare against.</param>
    /// <returns>True if the option value matches, false otherwise.</returns>
    public sealed override bool Is(int @int) => @int == GetInt();

    /// <summary>
    /// Checks if the option value matches a specific float.
    /// </summary>
    /// <param name="@float">The float value to compare against.</param>
    /// <returns>True if the option value matches (as integer), false otherwise.</returns>
    public sealed override bool Is(float @float) => @float == GetFloat();
}