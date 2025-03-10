using System;
using WukongApi.Timer;

namespace WukongApi.UI
{
    public class TimerWidget : GameWidgetBase
    {
        public TimerWidget() : base(Constants.TimerWidgetName) 
        {
            _countdownTimer.OnTick += (int minutes, int seconds) => SetText(minutes, seconds);
        }

        private readonly CountdownTimer _countdownTimer = new(1, 5);

        private void SetText(int minutes, int seconds)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"SetText {minutes} {seconds}", true);
        }

        public void StartRoundCountdown(int minutes, int seconds, Action onFinishedCallback)
        {
            SetText(minutes, seconds);
            SetVisibility(true);
            _countdownTimer.SetTime(minutes, seconds);
            _countdownTimer.Start(() => { onFinishedCallback(); StopCountdown(); });
        }

        public void StopCountdown()
        {
            SetVisibility(false);
            _countdownTimer.Reset();
        }
    }
}
