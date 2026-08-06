using System;
using System.Collections.Generic;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api;

/// <summary>
/// API for PvP mode. Will be removed in the future when custom data sync and server-side scripting are implemented.
/// </summary>
[Obsolete("This API is temporary and will be removed in the future when custom data sync is implemented.")]
public interface IWukongPvpApi
{
    int LevelId { get; set; }
    bool InPvP { get; set; }
    bool InPvpTournament { get; set; }
    bool AntiStallEnabled { get; }
    bool OwnsPvpState { get; }
    bool ImmobilizeAllowed { get; }
    bool GourdAllowed { get; }
    bool ConsumablesAllowed { get; }
    int EnemiesNgPlusLevel { get; }
    int CurrentRound { get; }
    int TournamentRounds { get; }
    IEnumerable<int> RoundWinners { get; set; }
    void InitializeAreaPvpState();
    void SetLastRoundWinnerTeam(int winner);
    void SetIsReadyForPvp(ReadyMainCharacter mainCharacter, bool ready);
    bool IsReadyForPvP(ReadyMainCharacter mainCharacter);
}