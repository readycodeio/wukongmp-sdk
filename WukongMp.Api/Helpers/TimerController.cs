using WukongMp.Api.UI;

namespace WukongMp.Api.Helpers
{
    internal class TimerController(WukongWidgetManager widgetManager)
    {
        private CountdownTimer? _timer;
        private int _initialMinutes;
        private int _initialSeconds;

        public void SetTimer(int minutes, int seconds)
        {
            _initialMinutes = minutes;
            _initialSeconds = seconds;
            _timer = new CountdownTimer(minutes, seconds);
            widgetManager.SetTimerVisibility(true);
            widgetManager.SetTimerText(_initialMinutes, _initialSeconds);
        }

        public void StartTimer()
        {
            _timer?.Start(
                onFinishedCallback: OnTimerFinished,
                onTickCallback: OnTimerTick
            );
        }

        private void OnTimerTick(int minutes, int seconds)
        {
            widgetManager.SetTimerText(minutes, seconds);
        }

        private void OnTimerFinished()
        {
            widgetManager.SetTimerText(0, 0);
            widgetManager.SetTimerVisibility(false);
        }

        public void StopTimer()
        {
            _timer?.Stop();
        }

        public void ResetTimer()
        {
            _timer?.Reset();
            widgetManager.SetTimerVisibility(true);
            widgetManager.SetTimerText(_initialMinutes, _initialSeconds);
        }
    }
}
