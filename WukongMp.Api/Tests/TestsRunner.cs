using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WukongMp.Api.Tests
{
    public class TestsRunner(WukongEventBus eventBus, ILogger logger)
    {
        public bool IsRunning { get; private set; }

        private readonly WukongEventBus _eventBus = eventBus;
        private readonly ILogger _logger = logger;

        private readonly Queue<TestBase> _testsToRun = [];
        private TestBase? _currentTest;
        private List<Type> _allAvailableTestTypes = [];
        private Dictionary<string, Type> _allAvailableTestTypesDict = [];

        public void Init()
        {
            DiscoverAllTests();
            EnqueueAllTests();
        }

        /// <summary>
        /// Initialize TestsRunner with tests to run.
        /// </summary>
        /// <param name="testsToRun">';' delimited list of test names</param>
        public void Init(string testsToRun)
        {
            DiscoverAllTests();
            var testsNames = testsToRun.Split(';');
            foreach (var testName in testsNames)
            {
                EnqueueTest(testName);
            }
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
            _testsToRun.Clear();
            _currentTest = null;
        }

        public void Update(float deltaTime)
        {
            if (!IsRunning)
                return;

            if (!_eventBus.IsGameplayLevel)
                return;

            if (_currentTest == null)
            {
                if (_testsToRun.Count == 0)
                    return;

                _currentTest = _testsToRun.Dequeue();
                _logger.LogDebug("Starting test: {TestName}", _currentTest.GetType().Name);
            }

            var status = _currentTest.Update(deltaTime);

            if (status == TestState.Succeeded)
            {
                _logger.LogDebug("Test: {TestName} successfully finishsed.", _currentTest.GetType().Name);
                _currentTest = null;
            }
            else if (status == TestState.Failed)
            {
                _logger.LogError("Test: {TestName} failed. Description: {Description}", _currentTest.GetType().Name, _currentTest.Description);
                _currentTest = null;
            }
        }

        private void DiscoverAllTests()
        {
            var type = typeof(TestBase);
            _allAvailableTestTypes = [.. AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => type.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)];
            _allAvailableTestTypesDict = _allAvailableTestTypes.ToDictionary(t => t.Name);
        }

        private void EnqueueAllTests()
        {
            foreach (Type type in _allAvailableTestTypes)
            {
                EnueueTest(type);
            }
        }

        private void EnqueueTest(string testName)
        {
            if (_allAvailableTestTypesDict.TryGetValue(testName, out Type typeToCreate))
            {
                EnueueTest(typeToCreate);
            }
            else
            {
                _logger.LogError("Type {TestTypeName} not found or is not a valid TestBase implementation.", testName);
            }
        }

        private void EnueueTest(Type testType)
        {
            if (Activator.CreateInstance(testType) is TestBase instance)
            {
                _testsToRun.Enqueue(instance);
                _logger.LogDebug("Enqueued test: {TestName}", testType.Name);
            }
            else
            {
                _logger.LogError("Failed instantiating test: {TestName}", testType.Name);
            }
        }
    }
}
