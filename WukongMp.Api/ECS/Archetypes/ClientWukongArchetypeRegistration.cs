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

    public void Register(IArchetypeRegistry world)
    {
        TamerArchetype = world.RegisterArchetype(
            WukongComponentUtils.GetServerMonsterArchetype()
                .Add(new MappingComponent<AActor>(new AActor()))
                .Add<LocalTamerComponent>()
                .Add<MarkerComponent>());

        MainCharacterArchetype = world.RegisterArchetype(
            WukongComponentUtils.GetServerMainCharacterArchetype()
                .Add(new MappingComponent<AActor>(new AActor()))
                .Add<LocalMainCharacterComponent>()
                .Add<MarkerComponent>());
    }
}