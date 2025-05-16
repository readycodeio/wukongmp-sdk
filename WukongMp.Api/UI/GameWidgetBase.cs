using UnrealEngine.UMG;

namespace WukongMp.Api.UI
{
    public abstract class GameWidgetBase(string name)
    {
        protected UUserWidget? GameWidget;

        public void Initialize()
        {
            GameWidget = BlueprintUiUtils.GetWidget(name);
            if (GameWidget != null)
            {
                Logging.LogDebug("{Name} widget initialized!", name);
                PostInitialize();
            }
            else
            {
                Logging.LogError("Cannot initialize {Name} widget", name);
            }
        }

        protected abstract void PostInitialize();

        public virtual void SetVisibility(bool visible)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetWidgetVisibility {visible}", true);
        }

        public void Deinitialize()
        {
            GameWidget = null;
        }
    }
}
