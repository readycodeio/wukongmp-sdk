using WukongMp.Api.Configuration;

namespace WukongMp.Api.UI
{
    public class ErrorMessageWidget : GameWidgetBase
    {
        private static ErrorMessageWidget? _instance;
        public static ErrorMessageWidget Instance => _instance ??= new();

        private ErrorMessageWidget() : base(Constants.ErrorMessageWidgetName) { }

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
