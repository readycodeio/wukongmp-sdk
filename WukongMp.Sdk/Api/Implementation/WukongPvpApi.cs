using ReadyM.Api.State;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.State;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongPvpApi(WukongAreaState areaState, IClientEntityManager clientNetEntity, ClientWukongArchetypeRegistration wukongArchetype, WukongPlayerState playerState) : IWukongPvpApi
{
    public bool InPvP => areaState.PvpState?.InPvP ?? false;
    public bool InPvpTournament => areaState.PvpState?.InTournament ?? false;
    public bool OwnsPvpState => areaState.OwnsPvpState;
    public int EnemiesNgPlusLevel => areaState.CurrentArea?.Room.EnemiesNgPlusLevel ?? 0;
    public bool AntiStallEnabled => areaState.CurrentArea?.Room.AntiStallEnabled ?? false;

    public void InitializeAreaPvpState()
    {
        areaState.PvpStateEntity = clientNetEntity.CreateAreaEntity(wukongArchetype.PvPStateSingletonArchetype);
    }

    public ref PvPComponent PvpData(ReadyMainCharacter mainCharacter)
    {
        return ref mainCharacter.Entity.GetPvP();
    }
}