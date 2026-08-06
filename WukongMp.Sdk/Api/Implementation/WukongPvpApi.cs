using System;
using System.Collections.Generic;
using ReadyM.Api.State;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.State;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api.Implementation;

[Obsolete("This API is temporary and will be removed in the future when custom data sync is implemented.")]
internal sealed class WukongPvpApi(WukongAreaState areaState, IClientEntityManager clientNetEntity, ClientWukongArchetypeRegistration wukongArchetype, LaunchParameters launchParameters)
    : IWukongPvpApi
{
    public int LevelId { get; set; } = launchParameters.LevelId ?? 0;

    public bool InPvP
    {
        get => areaState.PvpState?.InPvP ?? false;
        set
        {
            if (areaState.OwnsPvpState)
            {
                areaState.OwnedPvpStateRef().InPvP = value;
            }
            else
            {
                Logging.LogWarning("WukongPvpApi: Attempted to set InPvP without owning PvP state. This change will not be synchronized.");
            }
        }
    }

    public bool InPvpTournament
    {
        get => areaState.PvpState?.InTournament ?? false;
        set
        {
            if (areaState.OwnsPvpState)
            {
                areaState.OwnedPvpStateRef().InTournament = value;
            }
            else
            {
                Logging.LogWarning("WukongPvpApi: Attempted to set InPvpTournament without owning PvP state. This change will not be synchronized.");
            }
        }
    }

    public bool OwnsPvpState => areaState.OwnsPvpState;

    // TODO: These should be settable when not set from Agones
    public bool ImmobilizeAllowed => areaState.CurrentArea?.Room.ImmobilizeAllowed ?? true;
    public bool GourdAllowed => areaState.CurrentArea?.Room.GourdAllowed ?? true;
    public bool ConsumablesAllowed => areaState.CurrentArea?.Room.ConsumablesAllowed ?? true;
    public int EnemiesNgPlusLevel => areaState.CurrentArea?.Room.EnemiesNgPlusLevel ?? 0;
    public int CurrentRound => areaState.PvpState?.CurrentRound ?? 1;
    public int TournamentRounds => areaState.CurrentArea?.Room.TournamentRounds ?? 3;
    public bool AntiStallEnabled => areaState.CurrentArea?.Room.AntiStallEnabled ?? true;

    public void InitializeAreaPvpState()
    {
        areaState.PvpStateEntity = clientNetEntity.CreateAreaEntity(wukongArchetype.PvPStateSingletonArchetype);
    }

    public void SetLastRoundWinnerTeam(int winner)
    {
        if (areaState.OwnsPvpState)
        {
            areaState.OwnedPvpStateRef().SetLastRoundWinnerTeam(winner);
        }
        else
        {
            Logging.LogWarning("WukongPvpApi: Attempted to set last round winner team without owning PvP state. This change will not be synchronized.");
        }
    }

    public void SetIsReadyForPvp(ReadyMainCharacter mainCharacter, bool ready)
    {
        mainCharacter.Entity.GetPvP().IsReadyForPvP = ready;
    }

    public bool IsReadyForPvP(ReadyMainCharacter mainCharacter)
    {
        return mainCharacter.Entity.GetPvP().IsReadyForPvP;
    }

    public IEnumerable<int> RoundWinners
    {
        get => areaState.PvpState?.RoundWinners ?? [];
        set
        {
            if (areaState.OwnsPvpState)
            {
                areaState.OwnedPvpStateRef().RoundWinners = value;
            }
            else
            {
                Logging.LogWarning("WukongPvpApi: Attempted to set RoundWinners without owning PvP state. This change will not be synchronized.");
            }
        }
    }
}