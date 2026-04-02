namespace WukongMp.Api.UI
{
    internal class ErrorMessageWidget : GameWidgetBase
    {
        private const string ErrorMessageWidgetPath = "/Game/Mods/WukongMod/WBP_ErrorMessage.WBP_ErrorMessage_C";

        public ErrorMessageWidget() : base(ErrorMessageWidgetPath) { }

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
