using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using WukongMp.Api.Tests.TestActions;

namespace WukongMp.Api.Tests.TestActionSequences
{
    internal class AllGameplayTestsSequence : TestActionSequenceBase
    {
        private List<Type> _allAvailableTestTypes = [];

        public AllGameplayTestsSequence(ILogger logger) : base(logger)
        {
            EnqueueTestAction<EnterLevelTestAction>();
            DiscoverAllGameplayTests();
            EnqueueAllGameplayTests();
        }

        private void DiscoverAllGameplayTests()
        {
            var type = typeof(IGameplayTestAction);
            _allAvailableTestTypes = [.. AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => type.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)];
        }
        private void EnqueueAllGameplayTests()
        {
            foreach (Type type in _allAvailableTestTypes)
            {
                EnueueTestAction(type);
            }
        }
    }
}
