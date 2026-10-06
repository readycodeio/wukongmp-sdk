using System.Collections.Generic;
using ReadyM.Api.ECS.Components;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Archetypes;
using ReadyM.Wukong.Common.ECS.Registry;
using UnrealEngine.Engine;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Archetypes;

internal sealed class ClientWukongArchetypeRegistration : IArchetypeRegistration, IArchetypeShapeBindings
{
    public ArchetypeId TamerArchetype { get; private set; }
    public ArchetypeId MainCharacterArchetype { get; private set; }
    
    public IEnumerable<(string Shape, ArchetypeId Archetype)> Bindings =>
    [
        ("WukongMp.Sdk.Common.Archetypes.MainCharacter", MainCharacterArchetype),
        ("WukongMp.Sdk.Common.Archetypes.Tamer", TamerArchetype)
    ];

    public void Register(IArchetypeRegistry world)
    {
        TamerArchetype = world.RegisterArchetype(
            WukongComponentUtils.GetServerMonsterArchetype()
                .Add<MappingComponent<AActor>>()
                .Add<LocalTamerComponent>()
                .Add<MarkerComponent>());

        MainCharacterArchetype = world.RegisterArchetype(
            WukongComponentUtils.GetServerMainCharacterArchetype()
                .Add<MappingComponent<AActor>>()
                .Add<LocalMainCharacterComponent>()
                .Add<MarkerComponent>());
    }
}