using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using b1;
using b1.BGW;
using b1.Prediction;
using BtlB1;
using BtlShare;
using CSharpModBase;
using CSharpModBase.Input;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Plugins.EnhancedInput;
using UnrealEngine.Runtime;
using UnrealEngine.UMG;
using WukongMp.Common;
using FInputActionValue = b1.FInputActionValue;

namespace WukongCSharpMod
{
    public class MyMod : ICSharpMod
    {
        public string Name => "ModExample";
        public string Version => "0.0.1";

        private UUserWidget _chatWidget;

        private WukongClient _photon;
        private readonly Harmony _harmony = new Harmony("WukongMP");

        private readonly Dictionary<int, PlayerState> _connectedPlayers = new Dictionary<int, PlayerState>();
        private readonly Dictionary<byte, MonsterState> _monsters = new Dictionary<byte, MonsterState>();

        private FVector _savedPosition;

        public void Init()
        {
            WukongClient.Log("Init");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            WukongClient.Log("Patched with Harmony");

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.Z, () =>
            {
                Utils.TryRunOnGameThread(() =>
                {
                    WukongClient.Log("Alt + Z");
                    var events = BUS_EventCollectionCS.Get(GameUtils.GetControlledPawn());

                    var properties = events.GetType().GetProperties();
                    foreach (var prop in properties)
                    {
                        if (prop.Name.StartsWith("Evt_"))
                        {
                            var propObj = prop.GetValue(events);

                            // check if it has a + operator for subscribing
                            // example: public static GSDel_Void_Bool operator +(GSDel_Void_Bool GSEvent, Del_Void_Bool Del)
                            var addMethod = propObj.GetType().GetMethod("op_Addition");

                            if (addMethod == null)
                                continue;

                            // cast del to the deleagte type expected by +=, e.g. Del_Void_Bool
                            var delType = addMethod.GetParameters()[1].ParameterType;

                            // list the parameter types of its invoke func
                            var invokeMethod = propObj.GetType().GetMethod("Invoke");

                            // if delType return type is not void, return
                            if (invokeMethod.ReturnType != typeof(void))
                            {
                                WukongClient.Log("Return type is not void");
                                continue;
                            }

                            var paramTypes = invokeMethod.GetParameters().Select(p => p.ParameterType).ToArray();

                            // use LINQ expression to create a delegate that calls GenericPrint with event name and its params
                            var parameters = paramTypes.Select(Expression.Parameter).ToArray();

                            var genericPrintArgs = new List<Expression> { Expression.Constant(prop.Name) };

                            var paramsArray = Expression.NewArrayInit(typeof(object), parameters.Select(p => Expression.Convert(p, typeof(object))));
                            genericPrintArgs.Add(paramsArray);

                            WukongClient.Log($"Subscribing to {prop.Name}");
                            WukongClient.Log($"Params: {string.Join(", ", paramTypes.Select(p => p.Name))}");

                            try
                            {
                                var call = Expression.Call(typeof(MyMod), nameof(GenericPrint), new Type[] { }, genericPrintArgs.ToArray());
                                var lambda = Expression.Lambda(call, parameters);
                                var del = lambda.Compile();

                                var castDel = Delegate.CreateDelegate(delType, del.Target, del.Method);

                                // subscribe to the event via addMethod, it's static
                                addMethod.Invoke(null, new object[] { propObj, castDel });
                            }
                            catch (Exception e)
                            {
                                WukongClient.Log($"Error subscribing to {prop.Name}: {e.Message}");
                            }
                        }
                    }
                });
            });

            // InitWorldCallbacks();

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                WukongClient.Log("Alt + X");

                InitPhoton();
                Connect();
            });

            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.H, () =>
            {
                WukongClient.Log("Alt + H");

                InitializeChatWidget();
                InitPhoton();
            });
        }

        private static readonly object Lock = new object();

        public static void GenericPrint(string name, object[] parameters)
        {
            var sb = new StringBuilder($"Calling {name} with args: ");
            foreach (var parameter in parameters)
            {
                sb.Append(parameter);
                sb.Append(", ");
            }

            WukongClient.Log(sb.ToString());
        }

        private void InitWorldCallbacks()
        {
            UWorld world = GameUtils.GetWorld();
            if (world != null)
            {
                BGW_EventCollection.Get(world).Evt_PostLoadMapWithWorld += OnMapLoaded;
                BGW_EventCollection.Get(world).Evt_PlayerDelayBeginPlayFinished += OnDelayBeginPlay;
            }
            else
            {
                WukongClient.Log("World is null.");
            }
        }

        private void InitPhoton()
        {
            var myLocation = GameUtils.GetControlledPawn().GetActorTransform().GetLocation();
            _photon = new WukongClient(SpawnPlayersAlreadyInRoom, myLocation.X, myLocation.Y, myLocation.Z);
            _photon.WukongChat.OnGetMessage += GetMessageFromWidget;
            _photon.WukongChat.OnConnectRequest += Connect;
        }

        private void OnMapLoaded()
        {
            UWorld world = GameUtils.GetWorld();
            if (world != null)
                WukongClient.Log($"Map loaded: {world.GetCurrentLevelName()}");
        }

        private void OnDelayBeginPlay()
        {
            WukongClient.Log("Delay begin play.");
            InitializeChatWidget();
            InitPhoton();
        }

        private void Connect()
        {
            if (_photon.Ready)
            {
                return;
            }

            _photon.OnPlayerJoined += (id, x, y, z) => Utils.TryRunOnGameThread(() => SpawnCloneForJoiningPlayer(id, x, y, z));
            _photon.OnKeyReceived += (id, key) => Utils.TryRunOnGameThread(() => ApplyPlayerInput(id, key));
            _photon.OnPlayerPosition += (id, data) => Utils.TryRunOnGameThread(() => ApplyPlayerPosition(id, data));
            _photon.OnUnitSpawn += (_, id, name, x, y, z) => Utils.TryRunOnGameThread(() => SpawnRemoteUnit(id, name, x, y, z));
            _photon.OnAttackRotation += (id, x, y, z, speed, force) => Utils.TryRunOnGameThread(() => ApplyAttackRotation(id, x, y, z, speed, force));
            _photon.OnRollSkill += (id, dir) => Utils.TryRunOnGameThread(() => ApplyRollSkill(id, (ESkillDirection)dir));
            _photon.OnMarkRolling += (id, rolling) => Utils.TryRunOnGameThread(() => ApplyMarkRolling(id, rolling));
            _photon.OnChangeDodgeSkill += (id, p1, p2) => Utils.TryRunOnGameThread(() => ApplyChangeDodgeSkill(id, p1, p2));
            _photon.OnRestartCombo += (id) => Utils.TryRunOnGameThread(() => ApplyRestartCombo(id));
            _photon.OnResetDodgeSkill += (id) => Utils.TryRunOnGameThread(() => ApplyResetDodgeSkill(id));
            _photon.OnJumpSkillCue += (id, input, x, y) => Utils.TryRunOnGameThread(() => ApplyJumpSkillCue(id, (ESkillDirection)input, x, y));
            _photon.OnStrideJump += (id, height) => Utils.TryRunOnGameThread(() => ApplyStrideJump(id, height));
            // _photon.OnFsmEvent += (id, tag) => Utils.TryRunOnGameThread(() => ApplyFsmEvent(id, tag));
            // _photon.OnUpdateFsmSolver += (id, p1) => Utils.TryRunOnGameThread(() => ApplyUpdateFsmSolver(id, p1));
            // _photon.OnSwitchFsmSolver += (id, newsolvertype) => Utils.TryRunOnGameThread(() => ApplySwitchFsmSolver(id, newsolvertype));
            _photon.WukongChat.OnSendMessage += AddMessageToWidget;
            _photon.WukongChat.OnSavePosition += SaveCurrentPosition;
            _photon.WukongChat.OnLoadPosition += LoadSavedPosition;
            _photon.WukongChat.OnSpawnEnemy += name => Utils.TryRunOnGameThread(() => SpawnEnemy(name));

            _photon.StartClient();
            SubscribeToPlayerEvents();
        }

        private void ApplySwitchFsmSolver(int id, byte newsolvertype)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                WukongClient.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            WukongClient.Log($"Applying FSM solver switch for player {id}");
            events.Evt_SwitchFsmSolver.Invoke((EFsmSolverType)newsolvertype);
        }

        private void ApplyUpdateFsmSolver(int id, float p1)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                WukongClient.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            WukongClient.Log($"Applying FSM solver update for player {id}");
            events.Evt_UpdateFsmSolver.Invoke(p1);
        }

        private void ApplyFsmEvent(int id, string tag)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                WukongClient.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            WukongClient.Log($"Applying FSM event for player {id}");
            events.Evt_TriggerFsmEvent.Invoke(new FGameplayTag(new FName(tag)));
        }

        private void SubscribeToPlayerEvents()
        {
            var myPawn = GameUtils.GetControlledPawn();
            var events = BUS_EventCollectionCS.Get(myPawn);
            events.Evt_TriggerInputActionImpl += SendInputEvents;
            events.Evt_AttackRotateToPos += SendAttackRotation; // important
            events.Evt_TriggerRollSkill += SendRollSkill;
            events.Evt_ReStartDodgeCombo += SendRestartCombo;
            events.Evt_ChangeDodgeSkill += SendChangeDodgeSkill;
            events.Evt_ResetDodgeSkill += SendResetDodgeSkill;
            events.Evt_MarkRolling += SendMarkRolling;
            events.Evt_TriggerJumpSkill.Cue += SendTriggerJumpSkillCue;
            events.Evt_TriggerFsmEvent += SendFsmEvent;
            events.Evt_UpdateFsmSolver += SendUpdateFsmSolver;
            events.Evt_SwitchFsmSolver += SendSwitchFsmSolver;
            // events.Evt_TriggerStrideJump += SendStrideJump;
        }

        private void SendSwitchFsmSolver(EFsmSolverType newsolvertype)
        {
            WukongClient.Log($"Sending FSM solver switch to server: {newsolvertype}");
            _photon.SendSwitchFsmSolver((byte)newsolvertype);
        }

        private void SendUpdateFsmSolver(float p1)
        {
            WukongClient.Log($"Sending FSM solver update to server: {p1}");
            _photon.SendUpdateFsmSolver(p1);
        }

        private void SendFsmEvent(FGameplayTag tag)
        {
            WukongClient.Log($"Sending FSM event to server: {tag}");
            _photon.SendFsmEvent(tag.TagName.ToString());
        }

        private void UnsubscribeFromPlayerEvents()
        {
            var myPawn = GameUtils.GetControlledPawn();
            var events = BUS_EventCollectionCS.Get(myPawn);
            events.Evt_TriggerInputActionImpl -= SendInputEvents;
            events.Evt_AttackRotateToPos -= SendAttackRotation;
            events.Evt_TriggerRollSkill -= SendRollSkill;
            events.Evt_ReStartDodgeCombo -= SendRestartCombo;
            events.Evt_ChangeDodgeSkill -= SendChangeDodgeSkill;
            events.Evt_ResetDodgeSkill -= SendResetDodgeSkill;
            events.Evt_MarkRolling -= SendMarkRolling;
            events.Evt_TriggerJumpSkill.Cue -= SendTriggerJumpSkillCue;
            // events.Evt_TriggerStrideJump -= SendStrideJump;
        }

        private void SendStrideJump(float height)
        {
            WukongClient.Log($"Sending stride jump to server: {height}");
            _photon.SendStrideJump(height);
        }

        private void ApplyStrideJump(int id, float height)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                WukongClient.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            WukongClient.Log($"Applying stride jump for player {id}");
            events.Evt_TriggerStrideJump.Invoke(height);
        }

        private void SendTriggerJumpSkillCue(ESkillDirection startjumpdir, FVector2D currentinput, GSPredictionKey predictionkey)
        {
            WukongClient.Log($"Sending jump skill cue to server: {startjumpdir}, {currentinput}");
            _photon.SendJumpSkillCue((byte)startjumpdir, currentinput.X, currentinput.Y);
        }

        private void ApplyJumpSkillCue(int id, ESkillDirection input, float x, float y)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                WukongClient.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            WukongClient.Log($"Applying jump skill cue for player {id}");
            events.Evt_TriggerJumpSkill.Cue.Invoke(input, new FVector2D(x, y));
        }

        private void SendResetDodgeSkill()
        {
            WukongClient.Log("Sending reset dodge skill to server");
            _photon.SendResetDodgeSkill();
        }

        private void ApplyResetDodgeSkill(int id)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                WukongClient.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            WukongClient.Log($"Applying reset dodge skill for player {id}");
            events.Evt_ResetDodgeSkill.Invoke();
        }

        private void SendChangeDodgeSkill(int p1, int p2)
        {
            WukongClient.Log($"Sending change dodge skill to server: {p1}, {p2}");
            _photon.SendChangeDodgeSkill(p1, p2);
        }

        private void ApplyChangeDodgeSkill(int id, int p1, int arg3)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                WukongClient.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            WukongClient.Log($"Applying change dodge skill for player {id}");
            events.Evt_ChangeDodgeSkill.Invoke(p1, arg3);
        }

        private void SendRestartCombo()
        {
            WukongClient.Log("Sending restart combo to server");
            _photon.SendReStartCombo();
        }

        private void ApplyRestartCombo(int id)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                WukongClient.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            WukongClient.Log($"Applying restart combo for player {id}");
            events.Evt_ReStartDodgeCombo.Invoke();
        }

        private void SendMarkRolling(bool p1)
        {
            WukongClient.Log($"Sending mark rolling to server: {p1}");
            _photon.SendMarkRolling(p1);
        }

        private void ApplyMarkRolling(int id, bool rolling)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                WukongClient.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            WukongClient.Log($"Applying mark rolling for player {id}");
            events.Evt_MarkRolling.Invoke(rolling);
        }

        private void SendRollSkill(ESkillDirection rolldir)
        {
            WukongClient.Log($"Sending roll skill to server: {rolldir}");
            _photon.SendRollSkill((byte)rolldir);
        }

        private void ApplyRollSkill(int id, ESkillDirection dir)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                WukongClient.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            WukongClient.Log($"Applying roll skill for player {id}");
            events.Evt_TriggerRollSkill.Invoke(dir);
        }

        private void SendAttackRotation(FVector targetlocation, float turnspeed, bool bforceupdate)
        {
            WukongClient.Log($"Sending attack rotation to server: {targetlocation}, {turnspeed}, {bforceupdate}");
            _photon.SendAttackRotation(targetlocation.X, targetlocation.Y, targetlocation.Z, turnspeed, bforceupdate);
        }

        private void ApplyAttackRotation(int id, float x, float y, float z, float speed, bool force)
        {
            if (!_connectedPlayers.TryGetValue((byte)id, out var player))
            {
                WukongClient.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            WukongClient.Log($"Applying attack rotation for player {id}");
            events.Evt_AttackRotateToPos.Invoke(new FVector(x, y, z), speed, force);
        }

        private void SpawnEnemy(string enemyName)
        {
            var unitName = UnitPathsConfig.GetUnitPath(enemyName);

            var loc = Global.CameraLookPosition;

            var id = (byte)_monsters.Count; // TODO: Overflow??
            var pawn = SpawnUnit(id, unitName, loc.X, loc.Y, loc.Z, false);

            _monsters[id] = new MonsterState
            {
                Id = id,
                Local = true,
                Pawn = pawn
            };

            WukongClient.Log($"Sending spawn enemy {enemyName} at {loc}");
            _photon.SpawnUnit(id, unitName, loc.X, loc.Y, loc.Z);
        }

        private BUTamerActor SpawnRemoteUnit(byte id, string unitName, float x, float y, float z)
        {
            return SpawnUnit(id, unitName, x, y, z, true);
        }

        private BUTamerActor SpawnUnit(byte id, string unitName, float x, float y, float z, bool remote)
        {
            WukongClient.Log($"Spawn unit called for {unitName}");

            if (string.IsNullOrEmpty(unitName))
                return null;

            var loc = new FVector(x, y, z);
            var rot = new FRotator();

            var world = GameUtils.GetWorld();

            UClass cachedResourceObj = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(unitName, ELoadResourceType.SyncLoadAndCache);
            AActor actor = BGUFunctionLibraryCS.BGUSpawnActor(world, (TSubclassOf<AActor>)cachedResourceObj, loc, rot);
            BUTamerActor buTamerActor = actor as BUTamerActor;
            FTamerRef currentRef = buTamerActor.CurrentRef;
            FieldInfo field = typeof(FTamerRef).GetField("_phase", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field.GetValue(currentRef).ToString() == "Dead")
                field.SetValue(currentRef, ETamerPhase.PreBegunPlay);
            currentRef.OverrideResetType = EBGUResetType.None;
            currentRef.GroupOverrideResetType = EBGUResetType.None;
            buTamerActor.TamerType = ETamerType.Spawned;
            WukongClient.Log("Spawned enemy: " + buTamerActor.GetName());

            if (!remote)
                return buTamerActor;

            _monsters.Add(id, new MonsterState
            {
                Id = id,
                Local = false,
                Pawn = buTamerActor
            });

            var events = BUS_EventCollectionCS.Get(buTamerActor);

            if (events is null)
            {
                WukongClient.Log("Events is null");
                return buTamerActor;
            }

            events.Evt_AIPerceptionSetting.Invoke(false);
            events.Evt_AIPauseBT.Invoke(true);

            return buTamerActor;
        }

        private void LoadSavedPosition()
        {
            var pawn = GameUtils.GetControlledPawn();
            if (pawn != null)
            {
                pawn.SetActorLocation(_savedPosition, false, out _, true);
            }
        }

        private void SaveCurrentPosition()
        {
            var pawn = GameUtils.GetControlledPawn();
            if (pawn != null)
            {
                _savedPosition = pawn.GetActorLocation();
            }
        }

        private void SpawnPlayersAlreadyInRoom()
        {
            var localTransform = GameUtils.GetControlledPawn().GetActorTransform().GetLocation();

            // when joining game, spawn all players already in room
            foreach (var id in _photon.GetOtherPlayersInRoom())
            {
                Utils.TryRunOnGameThread(() => SpawnCloneForJoiningPlayer(id, localTransform.X, localTransform.Y, localTransform.Z));
            }
        }

        private void SpawnCloneForJoiningPlayer(int id, float x, float y, float z)
        {
            if (_connectedPlayers.ContainsKey(id))
            {
                WukongClient.Log($"Player already exists: {id}");
                return;
            }

            UnsubscribeFromPlayerEvents();

            var controller = GameUtils.GetPlayerController();
            var playerPawnClass = GameUtils.GetControlledPawn().GetClass();
            var oldPawn = GameUtils.GetControlledPawn();
            var oldPawnPos = oldPawn.GetActorTransform().GetLocation();
            var oldPawnRot = oldPawn.GetActorTransform().GetRotation();

            BGUFuncLibPlayer.SpwanAndPossesPlayerContrlledPawn(controller, playerPawnClass, oldPawn.GetActorTransform(), pawn => { }, new BGUFuncLibPlayer.SpawnControlledPawnBlendParam
            {
                NeedBlend = false
            });

            // BGU_UnrealWorldUtil.DestroyActor(oldPawn);
            var clone = oldPawn;

            var cloneCharacter = clone as BGUPlayerCharacterCS;

            FActorSpawnParameters spawnInfo = new FActorSpawnParameters
            {
                Instigator = cloneCharacter.GetInstigator(),
                SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod.AlwaysSpawn,
                OverrideLevel = cloneCharacter.GetLevel(),
                ObjectFlags = EObjectFlags.Transient // We never want to save AI controllers into a map
            };

            var loc = cloneCharacter.GetActorLocation();
            var rot = cloneCharacter.GetActorRotation();

            // var @class = UClass.GetClass("BGPPlayerController"); // "BGPPlayerController" works for sure
            var @class = UClass.GetClass("BGUAIPlayerController"); // "BGPPlayerController" works for sure

            if (@class is null)
            {
                WukongClient.Log("Class is null");
                return;
            }

            var newController = GameUtils.GetWorld().SpawnActor(@class, ref loc, ref rot, ref spawnInfo);

            WukongClient.Log("Spawned new controller");

            if (newController != null && newController is ABGUAIPlayerController ctrl)
            {
                ctrl.Possess(clone);
                ctrl.CanBeDamaged = false;
                WukongClient.Log("Possessed new controller");
            }

            // assign in dictionary
            _connectedPlayers[id] = new PlayerState(id, clone);

            // teleport clone to cloneTransform
            var targetTransform = new FTransform(FRotator.ZeroRotator, new FVector(x, y, z));
            clone.SetActorTransform(targetTransform, false, out _, true);

            var controlledPawn = GameUtils.GetControlledPawn();
            controlledPawn.SetActorTransform(new FTransform(oldPawnRot, oldPawnPos), false, out _, false);

            SubscribeToPlayerEvents();
        }

        private void SendInputEvents(string actionname, ETriggerEvent triggerevent, FInputActionValue value)
        {
            KeyState keyState;
            PlayerInput key;

            // WukongClient.Log($"Action: {actionname}, TriggerEvent: {triggerevent}, Value: {value}");

            switch (actionname)
            {
                case "IA_B1MoveForward":
                case "IA_B1MoveSideways":
                    if (triggerevent == ETriggerEvent.Triggered)
                    {
                        var transform = GameUtils.GetControlledPawn().BGUGetActorTransform();
                        var pos = transform.GetLocation();
                        var rot = transform.GetRotation();
                        _photon.SendPositionUpdate(pos.X, pos.Y, pos.Z, rot.X, rot.Y, rot.Z, rot.W);
                    }
                    else if (triggerevent == ETriggerEvent.Completed)
                    {
                        // TODO: stopped moving, set to idle? (not really, 2 keys can be held at the same time)
                    }

                    return;
                case "IA_B1LightAttack":
                    key = PlayerInput.LightAttack;
                    keyState = triggerevent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                case "IA_B1HeavyAttack":
                    key = PlayerInput.HeavyAttack;
                    keyState = triggerevent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                case "IA_B1Walk":
                    key = PlayerInput.Walk;
                    keyState = triggerevent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                case "IA_B1Sprint_KB":
                    key = PlayerInput.Sprint;
                    keyState = triggerevent == ETriggerEvent.Started ? KeyState.Pressed : KeyState.Released;
                    break;
                default:
                    return;
            }

            _photon.SendKeyPressed(key, keyState);
        }

        private void ApplyPlayerPosition(int id, float[] data)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                WukongClient.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;
            var events = BUS_EventCollectionCS.Get(clone);

            // TODO: Set player.LastMovement
            var goal = new FVector(data[0], data[1], data[2]);
            var rotation = new FRotator(new FQuat(data[3], data[4], data[5], data[6]));
            try
            {
                // events.Evt_SetActorRotation.Invoke(rotation, false);
                // events.Evt_InterpolationMove.Invoke(goal, rotation, 0, true, false, false, true);
                events.Evt_AIMoveTo.Invoke(goal, null, player.MovementType, 10f, EBGUMoveAIType.None, false, false, "", "");
            }
            catch
            {
                // ignore
            }
        }

        private void AddMessageToWidget(bool isServerMesssage, string sender, string message)
        {
            if (_chatWidget != null)
            {
                WukongClient.Log($"Calling AddMessage function with message {message} from {sender}");
                _chatWidget.CallFunctionByNameWithArguments($"AddMessage {isServerMesssage} {sender} {message}", true);
            }
            else
            {
                InitializeChatWidget();
            }
        }

        private string GetMessageFromWidget()
        {
            if (_chatWidget != null)
            {
                _chatWidget.CallFunctionByNameWithArguments("GetSentMessage", true);
                var message = _chatWidget.ToolTipText.ToString();
                if (message.Length > 0)
                {
                    WukongClient.Log($"Got message: {message} in GetSentMessage funcition");
                }

                return message;
            }

            InitializeChatWidget();
            return "";
        }

        private void InitializeChatWidget()
        {
            var widgets = GameUtils.GetWidgets();
            if (widgets != null)
            {
                if (widgets.Count == 1)
                {
                    _chatWidget = widgets[0];
                    WukongClient.Log("Chat widget initialized!.");
                }
            }
        }

        private void ApplyPlayerInput(int id, KeyPress keyPress)
        {
            if (!_connectedPlayers.TryGetValue(id, out var player))
            {
                WukongClient.Log($"Player not found: {id}");
                return;
            }

            var clone = player.Pawn;

            var events = BUS_EventCollectionCS.Get(clone);

            WukongClient.Log($"Player {id} pressed key {keyPress.Key} with state {keyPress.State}");

            switch (keyPress.Key)
            {
                case PlayerInput.LightAttack:
                {
                    var prop = events.GetType().GetProperty("Evt_InputCastSkill");
                    var gottenPropObj = prop.GetValue(events);
                    var method = gottenPropObj.GetType().GetMethod("Invoke");
                    method.Invoke(gottenPropObj, new object[] { EInputActionType.LightAttack, keyPress.State == KeyState.Released, 0, -1, -1 });
                    break;
                }
                case PlayerInput.HeavyAttack:
                {
                    var prop = events.GetType().GetProperty("Evt_InputCastSkill");
                    var gottenPropObj = prop.GetValue(events);
                    var method = gottenPropObj.GetType().GetMethod("Invoke");
                    method.Invoke(gottenPropObj, new object[] { EInputActionType.HeavyAttack, keyPress.State == KeyState.Released, 0, -1, -1 });
                    break;
                }
                case PlayerInput.Walk:
                    player.MovementType = keyPress.State == KeyState.Pressed ? EAIMoveSpeedType.JOG : EAIMoveSpeedType.RUN;
                    break;
                case PlayerInput.Sprint:
                    player.MovementType = keyPress.State == KeyState.Pressed ? EAIMoveSpeedType.SPRINT : EAIMoveSpeedType.RUN;
                    break;
            }
        }

        public void DeInit()
        {
            WukongClient.Log("DeInit");
        }
    }
}