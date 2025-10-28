using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Registry;
using WukongMp.Api.ECS.Components;

namespace WukongMp.Api.ECS.Archetypes;

public class ClientWukongArchetypeRegistration : IArchetypeRegistration
{
    public ArchetypeId MonsterArchetype { get; private set; }
    public ArchetypeId MainCharacterArchetype { get; private set; }
    public ArchetypeId PvPStateSingletonArchetype { get; private set; }

    public void Register(Store world)
    {
        MonsterArchetype = world.RegisterArchetype(b =>
        {
            WukongComponentUtils.SetupServerMonsterArchetype(b);
            b.Add<LocalTamerComponent>();
            b.Add<MarkerComponent>();
        });

        MainCharacterArchetype = world.RegisterArchetype(b =>
        {
            WukongComponentUtils.SetupServerMainCharacterArchetype(b);
            b.Add<LocalMainCharacterComponent>();
        });

        PvPStateSingletonArchetype = world.RegisterArchetype(WukongComponentUtils.SetupServerPvpStateArchetype);
    }
}