using BetterAmongUs.Helpers;
using UnityEngine;

namespace BetterAmongUs.Modules.OptionItems.NoneOption;

/// <summary>
/// Represents a visual divider item used to separate option groups in the UI.
/// </summary>
internal sealed class OptionDividerItem : OptionItem
{
    /// <summary>
    /// Gets whether this option item loads values from persistent storage.
    /// </summary>
    internal override bool CanLoad => false;

    /// <summary>
    /// Gets whether this item represents a configurable option (false for dividers).
    /// </summary>
    internal override bool IsOption => false;

    /// <summary>
    /// Gets or sets the spacing distances above and below the divider.
    /// </summary>
    internal (float top, float bottom) Distance { get; set; }

    /// <summary>
    /// Creates a new divider option item.
    /// </summary>
    /// <param name="tab">The tab this divider belongs to.</param>
    /// <param name="topDistance">Spacing distance above the divider.</param>
    /// <param name="bottomDistance">Spacing distance below the divider.</param>
    /// <returns>A new OptionDividerItem instance.</returns>
    internal static OptionDividerItem Create(OptionTab tab, float topDistance = 0.26f, float bottomDistance = 0.50f)
    {
        var Item = new OptionDividerItem
        {
            Tab = tab,
            Distance = (topDistance, bottomDistance)
        };
        Item.CreateBehavior();

        return Item;
    }

    /// <summary>
    /// Creates the UI behavior for this divider item.
    /// </summary>
    private void CreateBehavior()
    {
        if (!GameSettingMenu.Instance)
            return;

        AllOptionsTemp.Add(this);
        CategoryHeaderMasked categoryHeaderMasked = UnityEngine.Object.Instantiate(Tab.AUTab.categoryHeaderOrigin, Tab.AUTab.settingsContainer);
        Obj = categoryHeaderMasked.gameObject;
        categoryHeaderMasked.transform.localScale = Vector3.one * 0.63f;
        categoryHeaderMasked.Background.gameObject.DestroyObj();
        categoryHeaderMasked.Title.DestroyObj();
        if (categoryHeaderMasked.Divider != null)
        {
            categoryHeaderMasked.Divider.material.SetInt(PlayerMaterial.MaskLayer, MaskLayer);
        }
        Tab.Children.Add(this);
    }

    /// <summary>
    /// Gets the string representation of the divider value (not applicable).
    /// </summary>
    /// <returns>NotImplementedException as dividers don't have values.</returns>
    /// <exception cref="NotImplementedException">Always thrown since dividers have no values.</exception>
    public sealed override string ValueAsString()
    {
        throw new NotImplementedException();
    }
}