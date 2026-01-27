using WukongMp.Api.UI;

namespace WukongMp.Api.Helpers
{
    public class TimerController
    {
        private readonly WukongWidgetManager _widgetManager;
        private CountdownTimer? _timer;
        private int _initialMinutes;
        private int _initialSeconds;

        public TimerController(WukongWidgetManager widgetManager)
        {
            _widgetManager = widgetManager;
        }

        public void SetTimer(int minutes, int seconds)
        {
            _initialMinutes = minutes;
            _initialSeconds = seconds;
            _timer = new CountdownTimer(minutes, seconds);
            _widgetManager.SetTimerVisibility(true);
            _widgetManager.SetTimerText(_initialMinutes, _initialSeconds);
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
            _widgetManager.SetTimerText(minutes, seconds);
        }

        private void OnTimerFinished()
        {
            _widgetManager.SetTimerText(0, 0);
            _widgetManager.SetTimerVisibility(false);
        }

        public void StopTimer()
        {
            _timer?.Stop();
        }

        public void ResetTimer()
        {
            _timer?.Reset();
            _widgetManager.SetTimerVisibility(true);
            _widgetManager.SetTimerText(_initialMinutes, _initialSeconds);
        }
    }
}
