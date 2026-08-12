using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Friflo.Json.Fliox;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.ECS.Components;

/// <summary>
/// Holds the state of the PvP mode, including settings and in-game state.
/// </summary>
[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct PvpStateComponent
{
    // settings
    private int _levelId;
    private int _tournamentRounds;
    private bool _gourdAllowed;
    private bool _consumablesAllowed;
    private bool _immobilizeAllowed;
    private bool _phantomRushAllowed;
    private int _enemiesNgPlusLevel;
    private bool _cheatsAllowed;
    private bool _antiStallEnabled;
    
    // in-game state
    private bool _inPvP;
    private bool _inTournament;
    private string? _roundWinnersEncoded;

    [Ignore]
    public IEnumerable<int> RoundWinners
    {
        get
        {
            var str = RoundWinnersEncoded;
            return str == null ? [] : str.Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(int.Parse);
        }
        set => RoundWinnersEncoded = string.Join(";", value);
    }

    public int CurrentRound => RoundWinners.Count() + 1;

    public void SetLastRoundWinnerTeam(int teamId)
    {
        var winners = RoundWinners.ToList();
        winners.Add(teamId);
        RoundWinners = winners;
    }
}