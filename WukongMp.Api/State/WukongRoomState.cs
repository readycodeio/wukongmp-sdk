using System;
using System.Linq;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.ECS.Components;
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
            var areaEntity = state.CurrentAreaEntity;
            if (!areaEntity.HasValue)
                return false;

            var areaComp = areaEntity.Value.GetComponent<AreaScopeComponent>();
            return areaComp.MasterClient == state.LocalPlayerId;
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

    public Entity? PvpStateEntity { get; set; }
    
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
            
            if (PvpStateEntity is { IsNull: true })
                PvpStateEntity = null;

            return PvpStateEntity?.GetComponent<PvpStateComponent>();
        }
    }
    
    public delegate void MutatePvpStateAction(ref PvpStateComponent pvpStateComponent);
    
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