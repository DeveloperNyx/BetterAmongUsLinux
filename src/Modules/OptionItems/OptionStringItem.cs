using BetterAmongUs.Helpers;

namespace BetterAmongUs.Modules.OptionItems;

/// <summary>
/// Represents an option item that selects from a list of string values.
/// </summary>
public class OptionStringItem : OptionItem<int>
{
    /// <summary>
    /// Gets or sets the valid range of indices for this string option.
    /// </summary>
    protected IntRange Range { get; set; } = new();

    /// <summary>
    /// Gets or sets the array of translation keys for the string options.
    /// </summary>
    protected string[] TranslatorStrings { get; set; } = [];

    /// <summary>
    /// Gets or sets whether this option includes a random selection.
    /// </summary>
    private bool CanBeRandom { get; set; }

    /// <summary>
    /// Creates a new string option item or returns an existing one with the same ID.
    /// </summary>
    /// <param name="id">The unique identifier for this option.</param>
    /// <param name="tab">The tab this option belongs to.</param>
    /// <param name="tranStr">The translation key for the option name.</param>
    /// <param name="tranStrings">Array of translation keys for the selectable string values.</param>
    /// <param name="defaultValue">The default index value.</param>
    /// <param name="parent">Optional parent option for hierarchical organization.</param>
    /// <param name="canBeRandom">Whether this option includes a random selection.</param>
    /// <returns>A new or existing OptionStringItem instance.</returns>
    /// <exception cref="ArgumentException">Thrown when tranStrings has less than 2 elements.</exception>
    internal static OptionStringItem Create(int id, OptionTab tab, string tranStr, string[] tranStrings, int defaultValue, OptionItem? parent = null, bool canBeRandom = false)
    {
        if (tranStrings.Length < 2)
        {
            throw new ArgumentException("tranStrings must have more then 1 string!");
        }

        if (GetOptionById(id) is OptionStringItem stringItem)
        {
            stringItem.CreateBehavior();
            return stringItem;
        }

        if (defaultValue < 0)
        {
            canBeRandom = true;
        }

        OptionStringItem Item = new();
        AllOptions.Add(Item);
        Item._id = id;
        Item.Tab = tab;
        Item.Translation = tranStr;
        Item.TranslatorStrings = tranStrings;
        Item.Range = new IntRange(0, tranStrings.Length - 1);
        Item.DefaultValue = !canBeRandom ? defaultValue : defaultValue + 1;
        Item.CanBeRandom = canBeRandom;
        if (canBeRandom)
        {
            var list = Item.TranslatorStrings.ToList();
            list.Insert(0, "Option.RandomWithColor");
            Item.TranslatorStrings = [.. list];
            Item.Range = new IntRange(0, list.Count - 1);
        }

        if (parent != null)
        {
            Item.Parent = parent;
            parent.Children.Add(Item);
        }

        Item.CreateBehavior();
        return Item;
    }

    /// <summary>
    /// Creates the UI behavior for this string option.
    /// </summary>
    protected sealed override void CreateBehavior()
    {
        TryLoad();
        if (!GameSettingMenu.Instance) return;
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
    /// Increases the string selection index.
    /// </summary>
    protected void Increase()
    {
        SetValue(Value + 1);
    }

    /// <summary>
    /// Decreases the string selection index.
    /// </summary>
    protected void Decrease()
    {
        SetValue(Value - 1);
    }

    /// <summary>
    /// Sets the string selection value, clamping it to the valid range.
    /// </summary>
    /// <param name="newValue">The new string index to set.</param>
    public sealed override void SetValue(int newValue)
    {
        newValue = Math.Clamp(newValue, Range.min, Range.max);
        base.SetValue(newValue);
    }

    /// <summary>
    /// Updates the visual appearance of the string option based on its current value.
    /// </summary>
    /// <param name="updateTabVisuals">Whether to update the parent tab visuals as well.</param>
    internal sealed override void UpdateVisuals(bool updateTabVisuals = true)
    {
        if (Option is NumberOption numberOption)
        {
            numberOption.PlusBtn.SetInteractable(false);
            numberOption.MinusBtn.SetInteractable(false);

            if (Value < Range.max)
            {
                numberOption.PlusBtn.SetInteractable(true);
            }
            if (Value > Range.min)
            {
                numberOption.MinusBtn.SetInteractable(true);
            }

            numberOption.ValueText.text = $"{ValueAsString()}";
        }

        if (updateTabVisuals)
        {
            Tab.UpdateVisuals();
        }
    }

    /// <summary>
    /// Gets the translated string representation of the current selection.
    /// </summary>
    /// <returns>The translated string for the current index.</returns>
    public override string ValueAsString() => Translator.GetString(TranslatorStrings[Value], showInvalid: false);

    /// <summary>
    /// Gets the effective string value index, accounting for random selection.
    /// </summary>
    /// <returns>The actual string index (random if selected).</returns>
    public sealed override int GetStringValue()
    {
        var value = GetValue();
        if (!CanBeRandom)
        {
            return value;
        }
        else
        {
            if (value == 0)
            {
                return TranslatorStrings.Skip(1).RandomIndex().index;
            }
            else
            {
                return value - 1;
            }
        }
    }

    /// <summary>
    /// Checks if the option's string value matches a specific string.
    /// </summary>
    /// <param name="@string">The string value to compare against.</param>
    /// <returns>True if the option value matches, false otherwise.</returns>
    public sealed override bool Is(string @string) => TranslatorStrings[Value] == @string || ValueAsString() == @string;

    /// <summary>
    /// Checks if the option's index value matches a specific integer.
    /// </summary>
    /// <param name="@int">The integer value to compare against.</param>
    /// <returns>True if the option value matches, false otherwise.</returns>
    public sealed override bool Is(int @int) => !CanBeRandom ? Value == @int : Value == @int - 1;
}