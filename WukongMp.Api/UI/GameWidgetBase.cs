using UnrealEngine.UMG;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.UI;

/// <summary>
/// Base class for UI widgets defined in .pak files.
/// </summary>
/// <param name="path">The path to the widget, relative to the "Content/UI" folder in the .pak files. For example, for a widget located at "Content/UI/MyWidget.uasset", the path would be "MyWidget".</param>
public abstract class GameWidgetBase(string path)
{
    protected UUserWidget? GameWidget;

    /// <summary>
    /// Initializes the widget by trying to find it first, and if it doesn't exist, spawns a new one.
    /// </summary>
    public void Initialize()
    {
        GameWidget = ModWidgetsUtils.GetWidget(path);
        if (GameWidget == null)
        {
            GameWidget = ModWidgetsUtils.SpawnWidget(path);
        }
        if (GameWidget != null)
        {
            Logging.LogDebug("{Name} widget initialized!", path);
            PostInitialize();
        }
        else
        {
            Logging.LogError("Cannot initialize {Name} widget", path);
        }
        GameWidget?.AddToViewport(1000);
        GameWidget?.SetVisibility(ESlateVisibility.SelfHitTestInvisible);
    }

    protected abstract void PostInitialize();

    /// <summary>
    /// Set the visibility of the widget.
    /// This requires the widget to be initialized first, otherwise it will do nothing.
    /// </summary>
    /// <param name="visible">Whether the widget should be visible or not.</param>
    public virtual void SetVisibility(bool visible)
    {
        GameWidget?.CallFunctionByNameWithArguments($"SetWidgetVisibility {visible}", true);
    }

    /// <summary>
    /// Removes the widget from the viewport and sets the reference to null.
    /// </summary>
    public void Deinitialize()
    {
        GameWidget?.RemoveFromParent();
        GameWidget = null;
    }
}