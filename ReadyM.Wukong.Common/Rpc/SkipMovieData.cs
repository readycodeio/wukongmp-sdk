using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.Rpc;

/// <summary>
/// Data structure for the RPC which is used to vote on skipping a cutscene in the game.
/// </summary>
[DeriveINetSerializable]
public partial struct SkipMovieData(
    int sequenceId,
    int waitingPlayers,
    int allPlayers) : INetSerializable
{
    public int SequenceId = sequenceId;
    public int WaitingPlayers = waitingPlayers;
    public int AllPlayers = allPlayers;
}