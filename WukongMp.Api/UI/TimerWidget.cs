using System;
using WukongMp.Api.Configuration;
using WukongMp.Api.Helpers;

namespace WukongMp.Api.UI
{
    public class TimerWidget : GameWidgetBase
    {
        private static TimerWidget? _instance;
        public static TimerWidget Instance => _instance ??= new();
        
        private TimerWidget() : base(Constants.TimerWidgetName)
        {
            _countdownTimer.OnTick += SetText;
        }

        private readonly CountdownTimer _countdownTimer = new(1, 5);

        private void SetText(int minutes, int seconds)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetText {minutes} {seconds}", true);
        }

        public void StartCountdown(int minutes, int seconds, Action onFinishedCallback)
        {
            SetText(minutes, seconds);
            SetVisibility(true);
            _countdownTimer.SetTime(minutes, seconds);
            _countdownTimer.Start(() =>
            {
                StopCountdown();
                onFinishedCallback();
            });
        }

        public void StopCountdown()
        {
            SetVisibility(false);
            _countdownTimer.Reset();
        }

        protected override void PostInitialize() { }
    }
}