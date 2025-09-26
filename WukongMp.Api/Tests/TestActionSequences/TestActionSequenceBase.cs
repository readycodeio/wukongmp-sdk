using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using WukongMp.Api.Tests.TestActions;

namespace WukongMp.Api.Tests.TestActionSequences
{
    public class TestActionSequenceBase(ILogger logger)
    {
        private readonly Queue<TestActionBase> _testsToRun = [];
        private readonly ILogger _logger = logger;

        public bool HasQueuedTests()
        {
            return _testsToRun.Count > 0;
        }

        public TestActionBase GetNextQueuedTestAction()
        {
            return _testsToRun.Dequeue();
        }

        public void Clear()
        {
            _testsToRun.Clear();
        }

        protected void EnqueueTestAction<TestActionType>(int repetitions = 1) where TestActionType : TestActionBase
        {
            EnueueTestAction(typeof(TestActionType), repetitions);
        }

        protected void EnueueTestAction(Type testType, int repetitions = 1)
        {
            if (Activator.CreateInstance(testType) is TestActionBase instance)
            {
                for (int i = 0; i < repetitions; i++)
                {
                    EnueueTestAction(instance);
                }
            }
            else
            {
                _logger.LogError("Failed instantiating test: {TestName}", testType.Name);
            }
        }

        protected void EnueueTestAction(TestActionBase testAction)
        {
            _testsToRun.Enqueue(testAction);
            _logger.LogDebug("Enqueued test: {TestName}", testAction.GetType().Name);
        }
    }
}
