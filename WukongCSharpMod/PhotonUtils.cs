using b1;
using BtlShare;
using UnrealEngine.Engine;

namespace WukongCSharpMod
{
    public class PhotonUtils
    {
        public static void DiscoverMonsters()
        {
            var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
            foreach (var actor in allActorsOfClass)
            {
                if (actor.GetMonster() != null)
                {
                    SyncMonsterAndNotify(MyMod.Instance.Photon, actor);
                }
            }
        }

        /// <summary>
        /// Register a spawned monster in Photon and notify other clients.
        /// If successful, the monster will be prepared for syncing.
        /// </summary>
        public static void SyncMonsterAndNotify(WukongClient photon, BUTamerActor tamer)
        {
            var monster = tamer.GetMonster();
            var guid = BGU_DataUtil.GetActorGuid(monster);

            // register in Photon if not present
            var monsterState = photon.GetByTamerActor(tamer);
            if (monsterState == null)
            {
                monsterState = new MonsterState(guid, tamer);
                Helpers.Log($"Registering local monster in Photon: {guid}");
                photon.SyncedMonsters.Add(guid, monsterState);

                // notify other clients
                photon.SendMonsterWakeUp(guid);
            }

            // sanity check guid
            if (monsterState.Guid != guid)
            {
                Helpers.LogError($"Guid mismatch: {monsterState.Guid} {guid}");
                return;
            }

            PrepareMonsterForSync(photon, monsterState);
        }

        /// <summary>
        /// Prepare a monster for syncing.
        /// </summary>
        public static void PrepareMonsterForSync(WukongClient photon, MonsterState monsterState)
        {
            if (monsterState.IsSynced)
            {
                Helpers.LogError("Attempting to prepare monster that is already synced.");
                return;
            }

            var monster = monsterState.Pawn.GetMonster();

            // sanity check
            if (monster is null)
            {
                Helpers.LogError("Monster is null");
                return;
            }

            if (photon.IsMasterClient)
            {
                // subscribe to events on master
                var events = BUS_EventCollectionCS.Get(monsterState.Pawn);
                events.Evt_PlayMontageCallback += (reason, montage, state) =>
                {
                    var montagePath = montage.GetPathName();
                    Helpers.Log($"Monster montage callback: {monsterState.Guid} {reason} {montagePath} {state}");
                    photon.SendMonsterMontageCallback(monsterState.Guid, reason, montagePath, state);
                };

                // also, set HP
                var attrs = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(monster);
                monsterState.Hp = attrs.GetFloatValue(EBGUAttrFloat.Hp);
            }
            else
            {
                // disable AI on clients
                var events = BUS_EventCollectionCS.Get(monster);

                if (events is null)
                {
                    Helpers.LogError("Events is null");
                    return;
                }

                events.Evt_AIPerceptionSetting.Invoke(false);
                events.Evt_AIPauseBT.Invoke(true);
                events.Evt_AIPauseFsm.Invoke(true);
                events.Evt_EnableCanUpdateHatred.Invoke(P1: false);
                events.Evt_EnableCanSetBT.Invoke(P1: false);

                Helpers.Log("Tamer actor disabled.");
            }

            // at this point the monster exists, so we set IsSpawned
            monsterState.IsSynced = true;
        }
    }
}