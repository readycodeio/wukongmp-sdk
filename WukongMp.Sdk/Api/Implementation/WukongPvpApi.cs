using System;
using ReadyM.Api.State;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.State;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api.Implementation;

[Obsolete("This API is temporary and will be removed in the future when custom data sync is implemented.")]
internal sealed class WukongPvpApi(WukongAreaState areaState, IClientEntityManager clientNetEntity, ClientWukongArchetypeRegistration wukongArchetype) : IWukongPvpApi
{
    public bool InPvP => areaState.PvpState?.InPvP ?? false;
    public bool InPvpTournament => areaState.PvpState?.InTournament ?? false;
    public bool OwnsPvpState => areaState.OwnsPvpState;
    public bool ImmobilizeAllowed => areaState.CurrentArea?.Room.ImmobilizeAllowed ?? false;
    public bool GourdAllowed => areaState.CurrentArea?.Room.GourdAllowed ?? false;
    public bool ConsumablesAllowed => areaState.CurrentArea?.Room.ConsumablesAllowed ?? false;
    public int EnemiesNgPlusLevel => areaState.CurrentArea?.Room.EnemiesNgPlusLevel ?? 0;
    public int CurrentRound => areaState.PvpState?.CurrentRound ?? 0;
    public int TournamentRounds => areaState.CurrentArea?.Room.TournamentRounds ?? 0;
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