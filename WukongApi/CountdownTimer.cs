using System;
using System.Timers;

namespace WukongApi
{
    public class CountdownTimer
    {
        private int _remainingSeconds;
        private int _totalSeconds;
        private readonly Timer _timer;

        public event Action<int, int> OnTick;
        public event Action OnFinished;

        public CountdownTimer(int minutes, int seconds)
        {
            _totalSeconds = minutes * 60 + seconds;
            _remainingSeconds = _totalSeconds;
            _timer = new Timer(1000);
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
                OnFinished?.Invoke();
            }
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Reset()
        {
            Stop();
            _remainingSeconds = _totalSeconds;
        }
    }
}
