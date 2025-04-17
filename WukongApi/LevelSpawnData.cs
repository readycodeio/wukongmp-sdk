using UnrealEngine.Runtime;

namespace WukongApi
{
    public struct LevelSpawnData(FVector pvpStartingLocation, float pvpRadius = 4000)
    {
        public FVector PvpStartingLocation { get; private set; } = pvpStartingLocation;
        public float PvpRadius { get; private set; } = pvpRadius;
    }
}
