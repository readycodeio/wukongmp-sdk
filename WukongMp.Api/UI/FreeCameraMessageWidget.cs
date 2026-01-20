namespace WukongMp.Api.UI
{
    public class FreeCameraMessageWidget : GameWidgetBase
    {
        private const string FreeCameraMessageWidgetPath = "/Game/Mods/WukongMod/WBP_FreeCameraMessage.WBP_FreeCameraMessage_C";

        public FreeCameraMessageWidget() : base(FreeCameraMessageWidgetPath) { }

        public void SetMessageText(string message)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetMessageText {message}", true);
        }

        protected override void PostInitialize()
        {
        }
    }
}
