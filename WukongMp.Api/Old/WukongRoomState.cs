using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Idents;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.Wukong.ECS.Components;

namespace WukongMp.Api.Old;

public class WukongRoomState(ClientState state)
{
    public bool InRoom
    {
        get
        {
            var areaEntity = state.CurrentAreaEntity;
            return !areaEntity.IsNull;
        }
    }

    public bool IsMasterClient
    {
        get
        {
            var areaEntity = state.CurrentAreaEntity;
            if (areaEntity.IsNull)
                return false;
            
            var roomComponent = areaEntity.GetComponent<WukongRoomComponent>();
            return roomComponent.MasterClient == state.LocalPlayerId;
        }
    }

    public PlayerId MasterClientId
    {
        get => CurrentRoom.MasterClient;
        set
        {
            var areaEntity = state.CurrentAreaEntity;
            if (areaEntity.IsNull)
                return;

            var roomComponent = areaEntity.GetComponent<WukongRoomComponent>();
            roomComponent.MasterClient = value;
            areaEntity.Set(roomComponent);
        }
    }
    
    private WukongRoomComponent _localRoomComponent;
    
    public ref WukongRoomComponent CurrentRoom
    {
        get
        {
            var areaEntity = state.CurrentAreaEntity;
            if (areaEntity.IsNull)
                return ref _localRoomComponent;
            return ref areaEntity.GetComponent<WukongRoomComponent>();
        }
    }
}