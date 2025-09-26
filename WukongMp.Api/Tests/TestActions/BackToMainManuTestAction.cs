using b1;
using UnrealEngine.Engine;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Tests.TestActions
{
    internal class BackToMainManuTestAction : TestActionBase
    {
        private enum InnerState
        {
            LeaveGame,
            WaitForMainMenu,
        }

        private InnerState _currentState;

        private string GetCurLevelName()
        {
            UWorld worldFromObj = UGSE_EngineFuncLib.GetWorldFromObj(GameUtils.GetWorld());
            if (worldFromObj != null)
            {
                return worldFromObj.GetName();
            }
            return "";
        }

        private void TransferState(InnerState nextState)
        {
            _currentState = nextState;
        }

        public override TestState Update(float deltaTime)
        {
            switch (_currentState)
            {
                case InnerState.LeaveGame:
                    BGW_EventCollection.Get(GameUtils.GetWorld()).Evt_BGW_TriggerGlobalFSMEvent(EGI_Global.BackToMainMenu);
                    TransferState(InnerState.WaitForMainMenu);
                    break;
                case InnerState.WaitForMainMenu:
                    if (GetCurLevelName() == "Startup_V2_P")
                    {
                        return TestState.Succeeded;
                    }
                    break;
                default:
                    return TestState.Failed;
            }
            return TestState.Running;
        }
    }
}
