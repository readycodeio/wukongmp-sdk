using WukongMp.Api.Configuration;

namespace WukongMp.Api.UI
{
    public class InfoMessageWidget : GameWidgetBase
    {
        private static InfoMessageWidget? _instance;
        public static InfoMessageWidget Instance => _instance ??= new();

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
