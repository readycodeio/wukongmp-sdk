using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api;

/// <summary>
/// API for PvP mode. Will be removed in the future when custom data sync is implemented.
/// </summary>
public interface IWukongPvpApi
{
    bool InPvP { get; }
    bool InPvpTournament { get; }
    bool AntiStallEnabled { get; }
    bool OwnsPvpState { get; }
    int EnemiesNgPlusLevel { get; }
    int CurrentRound { get; set; }
    int TournamentRounds { get; set; }
    void InitializeAreaPvpState();
    ref PvPComponent PvpData(ReadyMainCharacter mainCharacter);
}