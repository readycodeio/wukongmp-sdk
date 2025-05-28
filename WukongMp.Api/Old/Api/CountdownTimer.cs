using System;
using System.Timers;

namespace WukongMp.Api.Old.Api
{
    public class CountdownTimer
    {
        private int _remainingSeconds;
        private int _totalSeconds;
        private readonly System.Timers.Timer _timer;
        private Action? _callback;

        public event Action<int, int>? OnTick;

        public CountdownTimer(int minutes, int seconds)
        {
            _totalSeconds = minutes * 60 + seconds;
            _remainingSeconds = _totalSeconds;
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += TimerElapsed;
        }

        public void SetTime(int minutes, int seconds)
        {
            _totalSeconds = minutes * 60 + seconds;
            Reset();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                OnTick?.Invoke(_remainingSeconds / 60, _remainingSeconds % 60);
            }
            else
            {
                Stop();
                _callback?.Invoke();
            }
        }

        public void Start(Action onFinishedCallback)
        {
            _timer.Start();
            _callback = onFinishedCallback;
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Reset()
        {
            Stop();
            _callback = null;
            _remainingSeconds = _totalSeconds;
        }
    }
}
