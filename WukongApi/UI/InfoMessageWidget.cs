namespace WukongApi.UI
{
    public class InfoMessageWidget() : GameWidgetBase(Constants.InfoMessageWidgetName)
    {
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
