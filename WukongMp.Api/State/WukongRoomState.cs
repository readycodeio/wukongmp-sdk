using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;

namespace WukongMp.Api.State;

public class WukongAreaState(ClientState state)
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
        => CurrentArea?.ScopeComponent.MasterClient;
    
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
}