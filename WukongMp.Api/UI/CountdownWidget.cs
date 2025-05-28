using System;
using WukongMp.Api.GameApi;
using WukongMp.Api.GameApi.Configuration;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;

namespace WukongMp.Api.UI
{
    public class CountdownWidget : GameWidgetBase
    {
        public CountdownWidget() : base(Constants.CountdownWidgetName)
        {
            _countdownTimer.OnTick += (int _, int seconds) => SetText(seconds);
        }

        private readonly CountdownTimer _countdownTimer = new(1, 5);

        private void SetText(int seconds)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetText {seconds}", true);
        }

        public void StartLobbyCountdown(int seconds, Action callback)
        {
            SetText(seconds);
            SetVisibility(true);
            _countdownTimer.SetTime(0, seconds);
            _countdownTimer.Start(() =>
            {
                StopCountdown();
                callback();
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