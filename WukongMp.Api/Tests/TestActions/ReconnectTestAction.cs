namespace WukongMp.Api.Tests.TestActions
{
    internal class ReconnectTestAction : TestActionBase, IGameplayTestAction
    {
        private enum InnerState
        {
            WaitForInitialConnection,
            Reconnect,
            WaitForConnection,
        }

        private InnerState _currentState = InnerState.WaitForInitialConnection;

        public override TestState Update(float deltaTime)
        {
            switch (_currentState)
            {
                case InnerState.WaitForInitialConnection:
                    if (DI.Instance.AreaState.InRoom)
                    {
                        GoToState(InnerState.Reconnect);
                    }
                    break;
                case InnerState.Reconnect:
                    DI.Instance.Connection.Reconnect();
                    GoToState(InnerState.WaitForConnection);
                    break;
                case InnerState.WaitForConnection:
                    if (DI.Instance.AreaState.InRoom)
                    {
                        return TestState.Succeeded;
                    }
                    break;
                default:
                    return TestState.Failed;
            }
            ElapsedTime += deltaTime;
            if (ElapsedTime > Timeout)
            {
                Description = "Timeout";
                return TestState.Failed;
            }
            return TestState.Running;
        }

        private void GoToState(InnerState state)
        {
            _currentState = state;
        }
    }
}
