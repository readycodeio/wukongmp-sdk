namespace WukongApi.UI
{
    public class TimerWidget : GameWidgetBase
    {
        public TimerWidget() : base(Constants.TimerWidgetName) { }

        public void SetText(int minutes, int seconds)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"SetText {minutes} {seconds}", true);
        }
    }
}
