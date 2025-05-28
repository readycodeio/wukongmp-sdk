using UnrealEngine.Runtime;

namespace WukongMp.Api.GameApi.Configuration;

public struct LevelSpawnData(int mapId, int mapAreaId, int birthPointID, FVector pvpStartingLocation, float pvpRadius = 4000)
{
    public int MapId { get; private set; } = mapId;
    public int MapAreaId { get; private set; } = mapAreaId;
    public int BirthPointID { get; private set; } = birthPointID;
    public FVector PvpStartingLocation { get; private set; } = pvpStartingLocation;
    public float PvpRadius { get; private set; } = pvpRadius;
}