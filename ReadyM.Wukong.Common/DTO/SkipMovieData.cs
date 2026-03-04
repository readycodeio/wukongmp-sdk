using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Wukong.Common.DTO;

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