using Friflo.Engine.ECS;
using ReadyM.Api;
using WukongMp.Api.Old;

namespace WukongMp.Api;

public class WukongUpdateLoop(Store world, WukongPlayerPropertyManager playerProperty) : SystemUpdateLoop(world)
{
    // TODO: After we remove the Client, this will be removed as well, leaving just the call to Tick()
    public override void Tick(UpdateTick tick)
    {
        playerProperty.SetCachedPlayerProperties();
        base.Tick(tick);
    }
}