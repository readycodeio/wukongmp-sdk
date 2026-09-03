using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Relay.Client.State;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.State;

internal class WukongAreaState(ClientState state, ClientOwnershipManager clientOwnershipManager)
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

    public Entity? PvpStateEntity
    {
        get => field?.IsNull is true ? null : field;
        set;
    }

    public bool OwnsPvpState => PvpStateEntity.HasValue && clientOwnershipManager.OwnsEntity(PvpStateEntity.Value);
}