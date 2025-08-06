using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Components;

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
            if (areaEntity == null)
                // FIXME: Is this correct?
                return false;
            
            var roomComponent = areaEntity.Value.GetComponent<WukongRoomComponent>();
            return roomComponent.MasterClient == state.LocalPlayerId;
        }
    }

    public PlayerId? MasterClientId
        => CurrentArea?.GetRoom().MasterClient;
    
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