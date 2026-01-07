using UnrealEngine.UMG;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.UI
{
    public abstract class GameWidgetBase(string path)
    {
        protected UUserWidget? GameWidget;

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

        public virtual void SetVisibility(bool visible)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetWidgetVisibility {visible}", true);
        }

        public void Deinitialize()
        {
            GameWidget?.RemoveFromParent();
            GameWidget = null;
        }
    }
}
