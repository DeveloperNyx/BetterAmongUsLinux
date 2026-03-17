using BetterAmongUs.Data;
using BetterAmongUs.Data.Config;
using BetterAmongUs.Patches.Gameplay.UI.Settings;

namespace BetterAmongUs.Modules.OptionItems;

/// <summary>
/// Represents a preset option item that set the settings preset.
/// </summary>
internal sealed class OptionPresetItem : OptionStringItem
{
    internal override bool CanLoad => false;

    /// <summary>
    /// Creates a new preset item for the options menu. If an item with the preset ID already exists, 
    /// it reuses the existing item and sets up its behavior.
    /// </summary>
    /// <returns>The created or reused <see cref="OptionPresetItem"/> instance.</returns>
    internal static OptionPresetItem Create()
    {
        int id = int.MaxValue;

        if (GetOptionById(id) is OptionPresetItem stringItem)
        {
            stringItem.CreateBehavior();
            return stringItem;
        }

        OptionPresetItem Item = new();
        AllOptions.Add(Item);
        Item._id = id;
        Item.Tab = GameSettingsPatch.BetterSettingsTab;
        Item.Translation = "Setting.Presets";
        Item.TranslatorStrings = Enumerable.Repeat(string.Empty, 10).ToArray();
        Item.Range = new IntRange(0, 10);
        Item.DefaultValue = 0;
        Item.Value = BAUConfigs.SettingsPreset.Value;

        Item.CreateBehavior();
        return Item;
    }

    internal override void OnValueChange(int oldValue, int newValue)
    {
        BAUConfigs.SettingsPreset.Value = newValue;
        BetterDataManager.BetterGameSettingsFile = new();
        BetterDataManager.BetterGameSettingsFile.Init();
        foreach (var opt in AllOptions)
        {
            opt.TryLoad(true);
        }
        GameSettingsPatch.BetterSettingsTab.UpdateVisuals();
    }

    public sealed override string ValueAsString()
    {
        return Translator.GetString("Setting.Preset", [Value.ToString()]);
    }
}
