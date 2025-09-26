using Microsoft.Extensions.Logging;
using WukongMp.Api.Tests.TestActions;

namespace WukongMp.Api.Tests.TestActionSequences
{
    public class ReconnectTestsSequence : TestActionSequenceBase
    {
        public ReconnectTestsSequence(ILogger logger) : base(logger)
        {
            EnqueueTestAction<EnterLevelTestAction>();
            EnqueueTestAction<ReconnectTestAction>(2);
            EnqueueTestAction<BackToMainManuTestAction>();
        }
    }
}
