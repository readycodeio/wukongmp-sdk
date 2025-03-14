namespace WukongApi.UI
{
    public class InfoMessageWidget : GameWidgetBase
    {
        public InfoMessageWidget() : base(Constants.InfoMessageWidgetName) { }


        public void SetText(string message)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"SetText {message}", true);
        }

        public void ClearMessages()
        {
            SetText("");
        }

        protected override void PostInitialize() { }
    }
}
