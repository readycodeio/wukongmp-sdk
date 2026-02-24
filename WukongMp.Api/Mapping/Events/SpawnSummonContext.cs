using Friflo.Engine.ECS;
using UnrealEngine.Runtime;

namespace WukongMp.Api.Mapping.Events;

public readonly struct SpawnSummonContext(Entity? summonerEntity, FVector summonLocation)
{
    public readonly Entity? SummonerEntity = summonerEntity;
    public readonly FVector SummonLocation = summonLocation;
}