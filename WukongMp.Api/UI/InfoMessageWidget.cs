namespace WukongMp.Api.UI
{
    public class InfoMessageWidget : GameWidgetBase
    {
        private const string InfoMessageWidgetPath = "/Game/Mods/WukongMod/WBP_InfoMessage.WBP_InfoMessage_C";

        public InfoMessageWidget() : base(InfoMessageWidgetPath) { }

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
