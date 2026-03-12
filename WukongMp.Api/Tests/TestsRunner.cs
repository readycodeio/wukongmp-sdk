using Microsoft.Extensions.Logging;
using WukongMp.Api.Tests.TestActions;
using WukongMp.Api.Tests.TestActionSequences;

namespace WukongMp.Api.Tests
{
    internal class TestsRunner(ILogger logger)
    {
        public bool IsRunning { get; private set; }

        private readonly ILogger _logger = logger;

        private TestActionBase? _currentTestAction;

        private TestActionSequenceBase? _testActionSequence;

        public void Init(TestActionSequenceBase testActionSequence)
        {
            _testActionSequence = testActionSequence;
        }

        public void Start()
        {
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void Clear()
        {
            _testActionSequence?.Clear();
            _testActionSequence = null;
            _currentTestAction = null;
        }

        public void Update(float deltaTime)
        {
            if (!IsRunning || _testActionSequence == null)
                return;

            if (_currentTestAction == null)
            {
                if (!_testActionSequence.HasQueuedTests())
                    return;

                _currentTestAction = _testActionSequence.GetNextQueuedTestAction();
                _logger.LogDebug("Starting test action: {TestName}", _currentTestAction.GetType().Name);
            }

            var status = _currentTestAction.Update(deltaTime);

            if (status == TestState.Succeeded)
            {
                _logger.LogDebug("Test action: {TestName} successfully finishsed.", _currentTestAction.GetType().Name);
                _currentTestAction = null;
            }
            else if (status == TestState.Failed)
            {
                _logger.LogError("Test action: {TestName} failed. Description: {Description}", _currentTestAction.GetType().Name, _currentTestAction.Description);
                _currentTestAction = null;
            }
        }
    }
}
