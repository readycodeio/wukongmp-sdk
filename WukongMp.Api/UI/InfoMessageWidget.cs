using WukongMp.Api.Configuration;

namespace WukongMp.Api.UI
{
    public class InfoMessageWidget : GameWidgetBase
    {
        public InfoMessageWidget() : base(Constants.InfoMessageWidgetName) { }

        public void SetText(string message)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetText {message}", true);
        }

        public void ClearMessages()
        {
            SetText("");
        }

        protected override void PostInitialize() { }
    }
}
