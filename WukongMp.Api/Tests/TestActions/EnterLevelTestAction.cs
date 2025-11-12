using b1;
using b1.BGW;
using System;
using UnrealEngine.Engine;
using UnrealEngine.Plugins.EnhancedInput;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Api.Configuration;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Tests.TestActions
{
    internal class EnterLevelTestAction : TestActionBase
    {
        private enum InnerState
        {
            WXLogin,
            PreStartProcess,
            EnterMap,
            WaitForEnterMap,
            WaitForBeginPlay,
        }

        private InnerState _currentState;
        private bool _roll;
        private bool _loadMapCompleted;
        private UObject? _worldContext;
        private BGW_GameLifeTimeMgr? _gameLifeTimeMgr;

        private void TransferState(InnerState nextState)
        {
            _currentState = nextState;
        }

        private string GetCurLevelName()
        {
            UWorld worldFromObj = UGSE_EngineFuncLib.GetWorldFromObj(_worldContext);
            if (worldFromObj != null)
            {
                return worldFromObj.GetName();
            }
            return "";
        }

        private void OnPostLoadMapWithWorld()
        {
            _loadMapCompleted = true;
        }

        public override TestState Update(float deltaTime)
        {
            if (_worldContext == null)
            {
                _worldContext = GameUtils.GetWorld();
            }
            switch (_currentState)
            {
                case InnerState.WXLogin:
                    if (GetCurLevelName() == "WXLogin_P")
                    {
                        TransferState(InnerState.PreStartProcess);
                    }
                    else if (GetCurLevelName() == "Startup_V2_P")
                    {
                        TransferState(InnerState.EnterMap);
                    }
                    break;
                case InnerState.PreStartProcess:
                    {
                        if (GetCurLevelName() == "Startup_V2_P")
                        {
                            TransferState(InnerState.EnterMap);
                            break;
                        }
                        UGSE_UMGFuncLib.QAGetAllWidgetsOfClass(UGSE_EngineFuncLib.GetWorldFromObj(_worldContext), out var FoundWidgets2, UClass.GetClass<GSScrollBox>());
                        if (FoundWidgets2 != null && FoundWidgets2.Count > 0)
                        {
                            foreach (GSScrollBox item in FoundWidgets2)
                            {
                                if (!(item == null))
                                {
                                    item.SetScrollOffset(item.GetScrollOffsetOfEnd());
                                }
                            }
                        }
                        _roll = !_roll;
                        if (_roll)
                        {
                            BGW_EventCollection.Get(_worldContext).Evt_InjectInputTriggerEvent("IA_GSUIConfirm", ETriggerEvent.Started, b1.FInputActionValue.True);
                        }
                        else
                        {
                            BGW_EventCollection.Get(_worldContext).Evt_InjectInputTriggerEvent("IA_GSUIConfirm", ETriggerEvent.Completed, b1.FInputActionValue.False);
                        }
                        break;
                    }
                case InnerState.EnterMap:
                    if (_gameLifeTimeMgr == null)
                    {
                        _gameLifeTimeMgr = BGW_GameLifeTimeMgr.Get(_worldContext);
                    }
                    if (_gameLifeTimeMgr == null)
                    {
                        Description = "GameLifeTimeMgr == null";
                        return TestState.Failed;
                    }
                    if (_gameLifeTimeMgr.IsInFSMState(SGI_Global.MainMenu))
                    {
                        var uClass = BGW_PreloadAssetMgr.Get(_worldContext).TryGetCachedResourceObj<UClass>("WidgetBlueprint'/Game/00Main/UI/BluePrintsV3/StartGame/BUI_StartGame.BUI_StartGame_C'", ELoadResourceType.SyncLoadAndCache);
                        if (uClass == null)
                        {
                            break;
                        }
                        UWidgetLibrary.GetAllWidgetsOfClass(UGSE_EngineFuncLib.GetWorldFromObj(_worldContext), out var FoundWidgets, uClass, TopLevelOnly: false);
                        if (FoundWidgets.Count > 0)
                        {
                            BGW_EventCollection bGW_EventCollection = BGW_EventCollection.Get(_worldContext);
                            bGW_EventCollection.Evt_PostLoadMapWithWorld = (b1.EventDelDefine.Del_Void)Delegate.Combine(bGW_EventCollection.Evt_PostLoadMapWithWorld, new b1.EventDelDefine.Del_Void(OnPostLoadMapWithWorld));
                            BGW_EventCollection.Get(_worldContext).Evt_BGW_TriggerGlobalFSMEvent(EGI_Global.LoadArchive, new FSMInputData_GI_Global_SubG_GI_Loading_TravelLevel
                            {
                                ArchiveId = Constants.NewCharacterArchiveId
                            });
                            TransferState(InnerState.WaitForEnterMap);
                        }
                    }
                    break;
                case InnerState.WaitForEnterMap:
                    if (_loadMapCompleted)
                    {
                        BGW_EventCollection bGW_EventCollection2 = BGW_EventCollection.Get(_worldContext);
                        bGW_EventCollection2.Evt_PostLoadMapWithWorld = (b1.EventDelDefine.Del_Void)Delegate.Remove(bGW_EventCollection2.Evt_PostLoadMapWithWorld, new b1.EventDelDefine.Del_Void(OnPostLoadMapWithWorld))!;
                        TransferState(InnerState.WaitForBeginPlay);
                    }
                    break;
                case InnerState.WaitForBeginPlay:
                    if (DI.Instance.EventBus.IsGameplayLevel)
                        return TestState.Succeeded;
                    break;
                default:
                    return TestState.Failed;
            }
            return TestState.Running;
        }
    }
}
