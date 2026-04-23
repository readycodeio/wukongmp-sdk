using ReadyM.Api.ECS.Registry;
using ReadyM.Wukong.Common.ECS.Components;

namespace ReadyM.Wukong.Common.ECS.Registry;

internal class WukongAreaRegistration : IAreaComponentRegistration
{
    public void Register(IAreaComponentRegistry registry)
    {
        registry.RegisterComponent(new RoomComponent
        {
            ConsumablesAllowed = true,
            ImmobilizeAllowed = true,
            GourdAllowed = true,
            PhantomRushAllowed = true,
            CheatsAllowed = false,
            AntiStallEnabled = true,
            ChatEnabled = true,
            EnemiesNgPlusLevel = 0,
            LevelId = 0,
            TournamentRounds = 3
        });
        registry.RegisterComponent(new MovieComponent());
    }
}