using Friflo.Engine.ECS;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Jobs;

public readonly struct SpawnMonstersFromEcs() : IEach<TamerComponent, TranslationComponent>
{
    public void Execute(ref TamerComponent tamer, ref TranslationComponent translation)
    {
        if (tamer.Guid != null && tamer.UnitPath != null)
        {
            SpawningUtils.SpawnUnitLocallyByPath(tamer.Guid, tamer.UnitPath, translation.Position.ToFVector());
        }
    }
}
