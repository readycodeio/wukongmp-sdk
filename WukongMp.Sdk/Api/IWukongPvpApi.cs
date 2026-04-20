using System;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api;

/// <summary>
/// API for PvP mode. Will be removed in the future when custom data sync is implemented.
/// </summary>
[Obsolete("This API is temporary and will be removed in the future when custom data sync is implemented.")]
public interface IWukongPvpApi
{
    bool InPvP { get; }
    bool InPvpTournament { get; }
    bool AntiStallEnabled { get; }
    bool OwnsPvpState { get; }
    bool ImmobilizeAllowed { get; }
    bool GourdAllowed { get; }
    bool ConsumablesAllowed { get; }
    int EnemiesNgPlusLevel { get; }
    int CurrentRound { get; }
    int TournamentRounds { get; }
    void InitializeAreaPvpState();
    ref PvPComponent PvpData(ReadyMainCharacter mainCharacter);
}