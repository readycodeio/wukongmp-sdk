using ReadyM.Api.ECS.Registry;
using ReadyM.Wukong.Common.ECS.Components;

namespace ReadyM.Wukong.Common.ECS.Registry;

internal class WukongAreaRegistration : IAreaComponentRegistration
{
    public void Register(IAreaComponentRegistry registry)
    {
        registry.RegisterComponent(new RoomComponent
        {
            ChatEnabled = true,
            ConsumablesAllowed = true,
            GourdAllowed = true,
            ImmobilizeAllowed = true,
            PhantomRushAllowed = true,
            TournamentRounds = 3,
            AntiStallEnabled = true,
            CheatsAllowed = false,
        });
        registry.RegisterComponent<MovieComponent>();
    }
}