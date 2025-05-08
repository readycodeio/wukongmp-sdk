using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;
using ReadyM.Relay.Common.Wukong;
using EntityManager = ReadyM.Relay.Common.ECS.EntityManager;

namespace WukongApi;

public sealed partial class WukongClient
{
    private readonly EntityManager entityManager;
    private ArchetypeId monsterArchetype;

    private void DefineEcs()
    {
        monsterArchetype = entityManager.DefineArchetype(
            typeof(CharacterTranslationComponent),
            typeof(CharacterAnimationComponent),
            typeof(CharacterHealthComponent),
            typeof(CharacterNicknameComponent),
            typeof(CharacterGameIdComponent),
            typeof(TamerComponent)
        );
    }

    public void RunTickSystems()
    {
        SendUpdatedMonsterProperties();
    }

    private readonly Dictionary<int, EntityId> monsterEntities = new();

    public ref T GetEntityComponent<T>(int peerId) where T : struct
    {
        var entityId = monsterEntities[peerId];
        return ref entityManager.GetComponent<T>(entityId);
    }

    public EntityId RegisterMonster(int monsterId)
    {
        var entity = entityManager.CreateEntity(monsterArchetype);
        monsterEntities[monsterId] = entity;
        return entity;
    }

    public void SendUpdatedMonsterProperties()
    {
        var writer = new NetDataWriter();
        writer.PutCustomEventHeader(3, RelayClient.LocalPlayer.PeerId, RelayMode.Others, EventCaching.DoNotCache);

        entityManager.RunSystem((
            EntityId entity,
            ref CharacterTranslationComponent translation,
            ref CharacterAnimationComponent animation,
            ref CharacterHealthComponent health,
            ref CharacterNicknameComponent nickname,
            ref CharacterGameIdComponent gameId) =>
        {
            var anyDirty = translation.IsDirty || animation.IsDirty || health.IsDirty || nickname.IsDirty || gameId.IsDirty;

            if (!anyDirty)
                return;

            writer.Put(entity.Index);
            writer.Put(entity.Version);

            translation.WriteDelta(RelayClient, writer);
            translation.ClearDirty();

            animation.WriteDelta(RelayClient, writer);
            animation.ClearDirty();

            health.WriteDelta(RelayClient, writer);
            health.ClearDirty();

            nickname.WriteDelta(RelayClient, writer);
            nickname.ClearDirty();

            gameId.WriteDelta(RelayClient, writer);
            gameId.ClearDirty();
        });

        RelayClient.OpRaiseEventRaw(writer, DeliveryMethod.Unreliable);
    }
}