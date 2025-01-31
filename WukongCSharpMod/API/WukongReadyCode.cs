using System;
using System.Collections.Generic;
using b1;
using b1.BGW;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongCSharpMod.Patches;

namespace WukongCSharpMod.API
{
    public class WukongReadyCode
    {
        private struct CharacterEntry
        {
            public CharacterId id;
            public bool isControlled;
            public AActor actor;
            public APawn pawn;
            public bool destroyed;
        }

        private bool _alreadyInit;
        
        private readonly CharacterId _localWukongCharacter = new CharacterId(0);
        private readonly List<CharacterEntry> _characterEntries = new List<CharacterEntry>()
        {
            new CharacterEntry(),
        };

        public void Init()
        {
            if (_alreadyInit)
                return;
            _alreadyInit = true;
            WukongMP.Instance.Patch();
        }
        
        public void Deinit()
        {
            if (!_alreadyInit)
                return;
            _alreadyInit = false;
            WukongMP.Instance.Unpatch();
        }
        
        private void EnsureInit()
        {
            if (!_alreadyInit)
                throw new InvalidOperationException($"WukongReadyCode not initialized, call {nameof(Init)} first");
        }

        private CharacterId CreateCharacterEntry()
        {
            var characterIndex = _characterEntries.Count;
            var characterId = new CharacterId(characterIndex);

            var entry = new CharacterEntry()
            {
                id = characterId,
            };
            _characterEntries.Add(entry);

            return characterId;
        }
        
        public CharacterId GetLocalWukongCharacter()
        {
            var entry = _characterEntries[_localWukongCharacter.index];
            if (entry.id == default)
            {
                entry.id = _localWukongCharacter;
                var world = GameUtils.GetWorld();
                var controller = UGSE_EngineFuncLib.GetFirstLocalPlayerController(world);
                entry.actor = controller;
                entry.pawn = controller.GetControlledPawn();
                _characterEntries[_localWukongCharacter.index] = entry;
            }

            return _localWukongCharacter;
        }
        
        private void EnsureValidCharacter(CharacterId character, out CharacterEntry entry)
        {
            if (character.index < 0 || character.index >= _characterEntries.Count)
                throw new ArgumentException($"Invalid character id: {character}");
            entry = _characterEntries[character.index];
            if (entry.destroyed)
                throw new InvalidOperationException($"Character {character} is destroyed");
        }
        
        private void EnsureControlled(in CharacterEntry entry)
        {
            if (!entry.isControlled)
                throw new InvalidOperationException($"Character {entry.id} cannot be controlled");
        }
        
        public FVector GetPosition(CharacterId character)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);
            if (entry.pawn == null)
                throw new InvalidOperationException($"Character {character} is not spawned");

            return entry.pawn.GetActorLocation();
        }
        
        public FRotator GetRotation(CharacterId character)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);
            if (entry.pawn == null)
                throw new InvalidOperationException($"Character {character} is not spawned");

            return entry.pawn.GetActorRotation();
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
            
            var cachedResourceObj = BGW_PreloadAssetMgr.Get(world).TryGetCachedResourceObj<UClass>(unitPath, ELoadResourceType.SyncLoadAndCache);
            var transform = new FTransform(rotation, actualPos);
            var buTamerActor = UBGUFunctionLibrary.BGUBeginDeferredActorSpawnFromClass(
                world,
                (TSubclassOf<AActor>)cachedResourceObj, transform, ESpawnActorCollisionHandlingMethod.AdjustIfPossibleButAlwaysSpawn, 
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
            var entry = _characterEntries[characterId.index];

            entry.actor = buTamerActor;
            entry.pawn = buTamerActor.GetMonster();
            _characterEntries[characterId.index] = entry;
            
            var events = BUS_EventCollectionCS.Get(entry.actor);

            if (events is null)
            {
                Logging.LogError("Events is null");
            }
            else
            {
                events.Evt_AIPerceptionSetting.Invoke(false);
                events.Evt_AIPauseBT.Invoke(true);
                events.Evt_AIPauseFsm.Invoke(true);
                events.Evt_EnableCanUpdateHatred.Invoke(P1: false);
                events.Evt_EnableCanSetBT.Invoke(P1: false);
            }
            
            return characterId;
        }
        
        public void DestroyCharacter(CharacterId character)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);
            if (entry.actor == null)
                throw new InvalidOperationException($"Character {character} is not spawned");

            entry.actor.DestroyActor();
            entry.actor = null;
            entry.pawn = null;
            entry.destroyed = true;
            _characterEntries[character.index] = entry;
        }
        
        public void SendJump(CharacterId character)
        {
            EnsureInit();

            throw new NotImplementedException();
        }
        
        public void SendMoveTo(CharacterId character, FVector targetPos)
        {
            EnsureInit();
            EnsureValidCharacter(character, out var entry);
            EnsureControlled(entry);
            
            var events = BUS_EventCollectionCS.Get(entry.actor);
            events.Evt_AIMoveTo.Invoke(targetPos, null, EAIMoveSpeedType.RUN, 2f, EBGUMoveAIType.None, false, false, "", "");
        }
        
        public void RunOnGameThread(Action callback)
        {
            EnsureInit();
            GameLoopPatch.LoopOnGameThread(callback);
        }
    }
}