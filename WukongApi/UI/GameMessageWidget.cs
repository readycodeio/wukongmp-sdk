namespace WukongApi.UI
{
    public class GameMessageWidget : GameWidgetBase
    {
        public GameMessageWidget() : base(Constants.GameMessageWidgetName) { }


        public void SetMainText(string message)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"SetMainText {message}", true);
        }

        public void SetSecondText(string message)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"SetSecondText {message}", true);
        }

        public void SetThirdText(string message)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"SetThirdText {message}", true);
        }

        public void ClearMessages()
        {
            SetMainText("");
            SetSecondText("");
            SetThirdText("");
        }
    }
}
