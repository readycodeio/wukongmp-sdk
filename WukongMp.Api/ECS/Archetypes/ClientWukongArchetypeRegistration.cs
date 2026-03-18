using ReadyM.Api.ECS.Components;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Wukong.Common.ECS.Registry;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Archetypes;

internal sealed class ClientWukongArchetypeRegistration : IArchetypeRegistration
{
    public ArchetypeId TamerArchetype { get; private set; }
    public ArchetypeId MainCharacterArchetype { get; private set; }
    public ArchetypeId PvPStateSingletonArchetype { get; private set; }

    public void Register(Store world)
    {
        TamerArchetype = world.RegisterArchetype(b =>
        {
            WukongComponentUtils.SetupServerMonsterArchetype(b);
            b.Add(new MappingComponent<AActor>(new AActor()));
            b.Add<LocalTamerComponent>();
            b.Add<MarkerComponent>();
        });

        MainCharacterArchetype = world.RegisterArchetype(b =>
        {
            WukongComponentUtils.SetupServerMainCharacterArchetype(b);
            b.Add(new MappingComponent<AActor>(new AActor()));
            b.Add<LocalMainCharacterComponent>();
            b.Add<MarkerComponent>();
        });

        PvPStateSingletonArchetype = world.RegisterArchetype(WukongComponentUtils.SetupServerPvpStateArchetype);
    }
}