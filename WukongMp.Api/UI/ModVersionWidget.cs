using WukongMp.Api.Configuration;

namespace WukongMp.Api.UI
{
    public class ModVersionWidget : GameWidgetBase
    {
        public ModVersionWidget() : base(Constants.ModVersionWidgetName) { }

        public void SetVersionText(string version)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetVersionText {version}", true);
        }

        protected override void PostInitialize() { }
    }
}
