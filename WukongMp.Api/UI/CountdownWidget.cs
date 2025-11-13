using WukongMp.Api.Configuration;

namespace WukongMp.Api.UI
{
    public class CountdownWidget : GameWidgetBase
    {
        public CountdownWidget() : base(Constants.CountdownWidgetName) { }

        public void SetText(int seconds)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetText {seconds}", true);
        }

        protected override void PostInitialize() { }
    }
}