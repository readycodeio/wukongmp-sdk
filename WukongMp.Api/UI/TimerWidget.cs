using System;
using WukongMp.Api.GameApi;
using WukongMp.Api.GameApi.Configuration;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;

namespace WukongMp.Api.UI
{
    public class TimerWidget : GameWidgetBase
    {
        public TimerWidget() : base(Constants.TimerWidgetName)
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