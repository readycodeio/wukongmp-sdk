using System;
using System.Linq;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.State;

public class WukongAreaState(ClientState state, Store world, ClientOwnershipManager clientOwnershipManager)
{
    public bool InRoom
        => state.CurrentAreaId != null;

    public bool IsMasterClient
    {
        get
        {
            var masterClient = MasterClientId;
            return masterClient != null && masterClient == state.LocalPlayerId;
        }
    }

    public PlayerId? MasterClientId
        => CurrentArea?.Scope.MasterClient;

    public AreaEntity? CurrentArea
    {
        get
        {
            var areaEntity = state.CurrentAreaEntity;
            if (areaEntity == null)
                return null;
            return new AreaEntity(areaEntity.Value);
        }
    }

    private Entity? _pvpStateEntity;

    public Entity? PvpStateEntity
    {
        get => _pvpStateEntity?.IsNull is true ? null : _pvpStateEntity;
        set => _pvpStateEntity = value;
    }

    public PvpStateComponent? PvpState
    {
        get
        {
            if (!CurrentArea.HasValue)
                PvpStateEntity = null;

            if (!PvpStateEntity.HasValue && CurrentArea.HasValue)
            {
                PvpStateEntity = world
                    .Query<PvpStateComponent>()
                    .HasValue<InScopeComponent, Entity>(CurrentArea.Value.Entity)
                    .Entities.FirstOrDefault();
            }

            return PvpStateEntity?.GetComponent<PvpStateComponent>();
        }
    }

    public bool OwnsPvpState => PvpStateEntity.HasValue && clientOwnershipManager.OwnsEntity(PvpStateEntity.Value);

    public ref PvpStateComponent OwnedPvpStateRef()
    {
        if (!PvpStateEntity.HasValue)
            throw new InvalidOperationException("No PvP state entity available.");

        if (!clientOwnershipManager.OwnsEntity(PvpStateEntity.Value))
            throw new InvalidOperationException("Client does not own the PvP state entity.");

        return ref PvpStateEntity.Value.GetComponent<PvpStateComponent>();
    }
}