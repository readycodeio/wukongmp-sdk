using System.Reflection;
using ReadyM.Api;
using WukongMp.Api;
using WukongMp.Api.Configuration;

namespace WukongMp.Sdk.Api;

public static class ReadyM
{
    internal static GameplayConfiguration Configuration => DI.Instance.GameplayConfiguration;
    
    public static WukongClientApi Client { get; } = new(DI.Instance.WukongArchetype, DI.Instance.World, DI.Instance.State, DI.Instance.AreaState, DI.Instance.PlayerState, DI.Instance.PawnState, DI.Instance.ClientNetEntity, DI.Instance.RelayClient, DI.Instance.SaveRelay, DI.Instance.MappedEvent);
    public static WukongLocalApi Local { get; } = new(
        DI.Instance.EventBus,
        DI.Instance.WidgetManager,
        DI.Instance.CommandConsole,
        DI.Instance.GameplayEventRouter,
        DI.Instance.CommandRegistry,
        DI.Instance.EcsLoop);
    
    internal static PatcherBase GetPatcher(ModBase mod)
    {
        return new WukongPatcher(mod.GetType().Assembly, mod.Name, DI.Instance.Prelude);
    }
    
    internal static PatcherBase GetPatcher(Assembly assembly, string name)
    {
        return new WukongPatcher(assembly, name, DI.Instance.Prelude);
    }
}