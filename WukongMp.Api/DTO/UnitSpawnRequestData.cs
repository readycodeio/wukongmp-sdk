using ReadyM.Api.Multiplayer;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct UnitSpawnRequestData(string unitName, int count, int teamId)
{
    public readonly string UnitName = unitName;
    public readonly int Count = count;
    public readonly int TeamId = teamId;
}