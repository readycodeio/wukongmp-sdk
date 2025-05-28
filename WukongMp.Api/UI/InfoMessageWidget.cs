using WukongMp.Api.GameApi.Configuration;
using WukongMp.Api.Old;

namespace WukongMp.Api.UI
{
    public class InfoMessageWidget : GameWidgetBase
    {
        public static InfoMessageWidget Instance { get; } = new();

        private InfoMessageWidget() : base(Constants.InfoMessageWidgetName) { }

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
