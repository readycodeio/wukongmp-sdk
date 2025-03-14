using UnrealEngine.UMG;

namespace WukongApi.UI
{
    public abstract class GameWidgetBase
    {
        protected UUserWidget _gameWidget;
        private readonly string _name;

        protected GameWidgetBase(string name)
        {
            _name = name;
        }

        public void Initialize()
        {
            _gameWidget = BlueprintUIUtils.GetWidget(_name);
            if (_gameWidget != null)
            {
                Logging.LogDebug("{Name} widget initialized!", _name);
                PostInitialize();
            }
            else
            {
                Logging.LogError("Cannot initialize {Name} widget", _name);
            }
        }

        protected abstract void PostInitialize();

        public void SetVisibility(bool visible)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"SetWidgetVisibility {visible}", true);
        }

        public void Deinitialize()
        {
            _gameWidget = null;
        }
    }
}
