using System;
using System.Collections.Generic;
using System.Linq;
using b1;
using b1.BGW;
using BtlShare;
using CSharpModBase;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongApi.API
{
    public class WukongReadyCode
    {
        private struct CharacterEntry
        {
            public CharacterId Id;
            public bool IsProgramControl;
            public AActor Controller;
            public APawn Pawn;
            public bool Destroyed;
        }

        private bool alreadyInit;

        private readonly CharacterId localWukongCharacter = new CharacterId(0);

        private readonly List<CharacterEntry> characterEntries = new List<CharacterEntry>
        {
            new CharacterEntry(),
        };

        public void Init()
        {
            if (alreadyInit)
                return;
            alreadyInit = true;
            WukongMP.Instance.Patch();
        }

        public void Deinit()
        {
            if (!alreadyInit)
                return;
            alreadyInit = false;
            WukongMP.Instance.Unpatch();
        }

        private void EnsureInit()
        {
            if (!alreadyInit)
                throw new InvalidOperationException($"WukongReadyCode not initialized, call {nameof(Init)} first");
        }

        private CharacterId CreateCharacterEntry()
        {
            var characterIndex = characterEntries.Count;
            var characterId = new CharacterId(characterIndex);

            var entry = new CharacterEntry()
            {
                Id = characterId,
            };
            characterEntries.Add(entry);

            return characterId;
        }

        public CharacterId GetLocalWukongCharacter()
        {
            var entry = characterEntries[localWukongCharacter.index];
            if (entry.Id == default)
            {
                entry.Id = localWukongCharacter;
                var world = GameUtils.GetWorld();
                var controller = UGSE_EngineFuncLib.GetFirstLocalPlayerController(world);
                entry.Controller = controller;
                entry.Pawn = controller.GetControlledPawn();
                characterEntries[localWukongCharacter.index] = entry;
            }

            return localWukongCharacter;
        }

        public CharacterId GetByPawn(APawn pawn)
        {
            EnsureInit();
            return characterEntries.FirstOrDefault(entry => entry.Pawn.GetName() == pawn.GetName()).Id;
        }

        private void EnsureValidCharacter(CharacterId character, out CharacterEntry entry)
        {
            if (character.index < 0 || character.index >= characterEntries.Count)
                throw new ArgumentException($"Invalid character id: {character}");
            entry = characterEntries[character.index];
            if (entry.Destroyed)
                throw new InvalidOperationException($"Character {character} is destroyed");
        }

        private void EnsureControlled(in CharacterEntry entry)
        {
            if (!entry.IsProgramControl)
                throw new InvalidOperationException($"Character {entry.Id} cannot be controlled");
        }

        public FVector GetPosition(CharacterId character)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);
            if (entry.Pawn == null)
                throw new InvalidOperationException($"Character {character} is not spawned");

            return entry.Pawn.GetActorLocation();
        }

        public FRotator GetRotation(CharacterId character)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);
            if (entry.Pawn == null)
                throw new InvalidOperationException($"Character {character} is not spawned");

            return entry.Pawn.GetActorRotation();
        }

        public CharacterId SpawnCharacter(FVector position, FRotator rotation, string unitName)
        {
            EnsureInit();

            // trace vertically for spawn height
            var hit = BGUFuncLibSelectTargetsCS.LineTraceForHitWorldItem(GameUtils.GetWorld(), position,
                position - FVector.UpVector * Constants.MonsterSpawnTraceHeight, out var hitResultSimple);
            FVector actualPos;
            if (hit)
            {
                actualPos = hitResultSimple.HitLocation + FVector.UpVector * Constants.MonsterHalfHeight;
                Logging.LogDebug("Spawning enemy by line trace");
            }
            else
            {
                actualPos = position;
                Logging.LogDebug("Spawning enemy by player forward vector");
            }

            // spawn in a spiral around center point, separated by 100 units
            Logging.LogDebug($"Spawn unit called for '{unitName}'");

            if (string.IsNullOrEmpty(unitName))
                return default;

            var world = GameUtils.GetWorld();

            var unitPath = UnitPathsConfig.GetUnitPath(unitName);
            Logging.LogDebug($"Spawn unit path is '{unitPath}'");

            var cachedResourceObj = BGW_PreloadAssetMgr.Get(world)
                .TryGetCachedResourceObj<UClass>(unitPath, ELoadResourceType.SyncLoadAndCache);
            var transform = new FTransform(rotation, actualPos);
            var buTamerActor = UBGUFunctionLibrary.BGUBeginDeferredActorSpawnFromClass(
                world,
                (TSubclassOf<AActor>)cachedResourceObj, transform,
                ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn,
                null
            ) as BUTamerActor;
            if (buTamerActor == null)
            {
                Logging.LogError($"Could not spawn enemy: '{unitName}'");
                return default;
            }

            var guid = Guid.NewGuid().ToString(); // TODO: use ActorGuid
            buTamerActor.SpawnedTamerGuid = guid;

            // Update final guid
            buTamerActor.GetFinalGuid();

            UBGUFunctionLibrary.BGUFinishSpawningActor(buTamerActor, transform);
            Logging.LogDebug($"Spawned enemy: {buTamerActor.GetName()}, with guid {guid}");

            var characterId = CreateCharacterEntry();
            var entry = characterEntries[characterId.index];

            entry.Controller = buTamerActor;
            entry.Pawn = null; // Not yet ready
            characterEntries[characterId.index] = entry;

            return characterId;
        }

        public CharacterId SpawnWukong(FVector position, FRotator rotation)
        {
            EnsureInit();

            // trace vertically for spawn height
            var hit = BGUFuncLibSelectTargetsCS.LineTraceForHitWorldItem(GameUtils.GetWorld(), position,
                position - FVector.UpVector * Constants.MonsterSpawnTraceHeight, out var hitResultSimple);
            FVector actualPos;
            if (hit)
            {
                actualPos = hitResultSimple.HitLocation + FVector.UpVector * Constants.MonsterHalfHeight;
                Logging.LogDebug("Spawning enemy by line trace");
            }
            else
            {
                actualPos = position;
                Logging.LogDebug("Spawning enemy by player forward vector");
            }

            var playerPawnClass = GameUtils.GetControlledPawn().GetClass();
            var oldPawn = GameUtils.GetControlledPawn();

            var @class = UClass.GetClass("BGUAIPlayerController");

            var oldController = GameUtils.GetPlayerController();
            var newPawn = WukongMP.SpawnWukong(oldController, playerPawnClass, new FTransform(rotation, actualPos), oldPawn);

            WukongMP.BackToOldPawn(oldController, oldPawn, newPawn);

            var newControllerActor = GameUtils.GetWorld().SpawnActor(@class, ref actualPos, ref rotation);
            if (newControllerActor != null && newControllerActor is ABGUAIPlayerController newController)
            {
                Logging.LogDebug("Spawned new controller");
                newController.Possess(newPawn);
            }

            // Reset falling timer.
            var events = BUS_EventCollectionCS.Get(newPawn);
            events.Evt_OnLeaveFalling.Invoke();
            events = BUS_EventCollectionCS.Get(oldPawn);
            events.Evt_OnLeaveFalling.Invoke();

            var characterId = CreateCharacterEntry();
            var entry = characterEntries[characterId.index];

            entry.Controller = newControllerActor;
            entry.Pawn = newPawn;
            characterEntries[characterId.index] = entry;

            return characterId;
        }

        private bool GetCharacterReady(ref CharacterEntry entry, out bool result)
        {
            if (entry.Pawn != null)
            {
                result = true;
                return false;
            }

            var changed = false;
            if (entry.Controller is BUTamerActor buTamerActor)
            {
                entry.Pawn = buTamerActor.GetMonster();
                changed = true;
            }

            result = entry.Pawn != null;
            return changed;
        }

        public bool IsCharacterReady(CharacterId character)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);

            if (GetCharacterReady(ref entry, out var result))
            {
                characterEntries[character.index] = entry;
            }

            return result;
        }

        private void EnsureCharacterReady(ref CharacterEntry entry)
        {
            if (GetCharacterReady(ref entry, out var result))
            {
                characterEntries[entry.Id.index] = entry;
            }

            if (!result)
                throw new InvalidOperationException($"Character {entry.Id} is not ready, wait a few frames");
        }

        public void ControlCharacter(CharacterId character)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);
            EnsureCharacterReady(ref entry);

            if (entry.IsProgramControl)
                return;

            entry.IsProgramControl = true;
            var events = BUS_EventCollectionCS.Get(entry.Pawn);

            if (events is null)
            {
                Logging.LogError("Events is null in ControlCharacter");
            }
            else
            {
                events.Evt_AIPerceptionSetting.Invoke(false);
                events.Evt_AIPauseBT.Invoke(true);
                events.Evt_AIPauseFsm.Invoke(true);
                events.Evt_EnableCanUpdateHatred.Invoke(P1: false);
                events.Evt_EnableCanSetBT.Invoke(P1: false);
            }

            characterEntries[character.index] = entry;
        }

        public void DestroyCharacter(CharacterId character)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);
            if (entry.Controller == null)
                throw new InvalidOperationException($"Character {character} is not spawned");

            entry.Controller.DestroyActor();
            entry.Controller = null;
            entry.Pawn = null;
            entry.Destroyed = true;
            characterEntries[character.index] = entry;
        }

        public void UpdatePawn(CharacterId character, APawn pawn)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);

            entry.Controller = pawn.GetController();
            entry.Pawn = pawn;
            characterEntries[character.index] = entry;
        }

        public void SendLightAttack(CharacterId character)
        {
            SendSkill(character, EInputActionType.LightAttack);
        }

        public void SendHeavyAttack(CharacterId character)
        {
            SendSkill(character, EInputActionType.HeavyAttack);
        }

        public void SendDodge(CharacterId character)
        {
            SendSkill(character, EInputActionType.Dodge);
        }
        
        public void SendTransform(CharacterId character, TransformKind kind)
        {
            // EnsureControlled(entry);

            // var events = BPS_GSEventCollection.Get(entry.Pawn.PlayerState);
            // events.Evt_TriggerPlayerTransBegin.Invoke(EPlayerTransBeginType.AddBuff, new PlayerTransParam
            // {
            //     NeedBlend = true,
            //     SpawnSkillId = 0,
            //     TransSkillId = 0,
            //     TargetResId = (int)kind
            // });
            
            SendSkill(character, EInputActionType.UseSkillByType, (int)kind, 260);
        }

        public void SendMoveTo(CharacterId character, FVector targetPos)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);
            EnsureControlled(entry);

            var events = BUS_EventCollectionCS.Get(entry.Pawn);

            if (events is null)
            {
                Logging.LogError("Events is null in SendMoveTo");
                return;
            }

            events.Evt_AIMoveTo.Invoke(targetPos, null, EAIMoveSpeedType.SPRINT, 2f, EBGUMoveAIType.KeepFacingTarget, false, false, "", "");
        }

        public void SendSkillRingOfFire(CharacterId character)
        {
            SendSkill(character, EInputActionType.UseSkillByType, 10520, 250, -1, true, true);
        }

        public void SendSkillSpellBinder(CharacterId character)
        {
            SendSkill(character, EInputActionType.UseSkillByType, 10521, 250, -1, true, true);
        }

        public void SendSkillRockSolid(CharacterId character)
        {
            SendSkill(character, EInputActionType.UseSkillByType, 10505, 230, -1, true, true);
        }

        public void SendSkillPluckOfMany(CharacterId character)
        {
            SendSkill(character, EInputActionType.UseSkillByType, 10516, 240, -1, true, true);
        }

        public void SendGourdPotion(CharacterId character)
        {
            SendSkill(character, EInputActionType.CastItemSkill, 10530, -1, -1, true, true);
        }

        public void SendOnHandsEvilRepellingMedicament(CharacterId character)
        {
            SendSkill(character, EInputActionType.CastItemSkill, 10913, -1, 2247, true, true);
        }

        public void SendOnHandsLongevityDecoction(CharacterId character)
        {
            SendSkill(character, EInputActionType.CastItemSkill, 10913, -1, 2230, true, true);
        }

        public void SendOnHandsTigerSubduingPellets(CharacterId character)
        {
            SendSkill(character, EInputActionType.CastItemSkill, 10530, -1, -1, true, true);
        }

        public void RunOnGameThread(Action callback)
        {
            EnsureInit();
            Utils.TryRunOnGameThread(callback);
        }

        private void SendSkill(CharacterId character, EInputActionType actionType, int skillID = 0, int descID = -1, int itemID = -1, bool resetCooldown = false, bool resetMana = false)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);
            EnsureControlled(entry);

            var events = BUS_EventCollectionCS.Get(entry.Pawn);

            if (events is null)
            {
                Logging.LogError("Events is null in SendSkill");
                return;
            }

            events.Evt_InputCastSkill.Invoke(actionType, false, skillID, descID, itemID);

            if (resetCooldown)
            {
                ResetCooldown(character);
            }

            if (resetMana)
            {
                ResetManaPoints(character);
            }
        }

        private void ResetCooldown(CharacterId character)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);

            var events = BUS_EventCollectionCS.Get(entry.Pawn);
            events.Evt_ResetSkillCD.Invoke();
        }

        private void ResetManaPoints(CharacterId character)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);

            var attrContainer = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(entry.Pawn);
            float maxMana = attrContainer.GetFloatValue(EBGUAttrFloat.MpMax);
            BUS_EventCollectionCS.Get(entry.Pawn)?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.Mp, maxMana);
        }
    }
}