using System;

namespace WukongApi.UI
{
    public class CountdownWidget : GameWidgetBase
    {
        public CountdownWidget() : base(Constants.CountdownWidgetName)
        {
            _countdownTimer.OnTick += (int _, int seconds) => SetText(seconds);
        }

        private readonly CountdownTimer _countdownTimer = new(1, 5);

        public void SetText(int seconds)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"SetText {seconds}", true);
        }

        public void StartLobbyCountdown(int seconds, Action callback)
        {
            SetText(seconds);
            SetVisibility(true);
            _countdownTimer.SetTime(0, seconds);
            _countdownTimer.Start(() => { callback(); StopCountdown(); });
        }

        public void StopCountdown()
        {
            SetVisibility(false);
            _countdownTimer.Reset();
        }
    }
}
