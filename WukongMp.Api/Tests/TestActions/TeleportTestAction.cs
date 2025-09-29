using b1;
using System;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Tests.TestActions
{
    internal class TeleportTestAction(int rebirthPointId) : TestActionBase
    {
        private readonly int _rebirthPointId = rebirthPointId;

        private enum InnerState
        {
            Delay,
            TeleportNewLevel,
            WaitForNewLevel,
            WaitForAreaConnection,
        }

        private InnerState _currentState;
        private bool _loadMapCompleted;

        public override TestState Update(float deltaTime)
        {
            switch (_currentState)
            {
                case InnerState.Delay:
                    if (ElapsedTime > 20)
                    {
                        TransferState(InnerState.WaitForNewLevel);
                    }
                    break;
                case InnerState.TeleportNewLevel:
                    var world = GameUtils.GetWorld();
                    BGW_EventCollection bGW_EventCollection = BGW_EventCollection.Get(world);
                    bGW_EventCollection.Evt_PostLoadMapWithWorld = (b1.EventDelDefine.Del_Void)Delegate.Combine(bGW_EventCollection.Evt_PostLoadMapWithWorld, new b1.EventDelDefine.Del_Void(OnPostLoadMapWithWorld));
                    BPS_EventCollectionCS.Get(GameUtils.GetControlledPawn()?.PlayerState).Evt_BPS_TeleportTo.Invoke(ETeleportTypeV2.RebirthPointTeleportOnly, new TeleportParam_RebirthPoint
                    {
                        RebirthPointId = _rebirthPointId
                    }, EPlayerTeleportReason.RebirthPoint);
                    TransferState(InnerState.WaitForNewLevel);
                    break;
                case InnerState.WaitForNewLevel:
                    if (_loadMapCompleted)
                    {
                        BGW_EventCollection bGW_EventCollection2 = BGW_EventCollection.Get(GameUtils.GetWorld());
                        bGW_EventCollection2.Evt_PostLoadMapWithWorld = (b1.EventDelDefine.Del_Void)Delegate.Remove(bGW_EventCollection2.Evt_PostLoadMapWithWorld, new b1.EventDelDefine.Del_Void(OnPostLoadMapWithWorld));
                        TransferState(InnerState.WaitForAreaConnection);
                    }
                    break;
                case InnerState.WaitForAreaConnection:
                    if (DI.Instance.AreaState.InRoom)
                    {
                        return TestState.Succeeded;
                    }
                    break;
                default:
                    return TestState.Failed;
            }
            ElapsedTime+=deltaTime;
            return TestState.Running;
        }

        private void TransferState(InnerState nextState)
        {
            _currentState = nextState;
        }

        private void OnPostLoadMapWithWorld()
        {
            _loadMapCompleted = true;
        }
    }
}
