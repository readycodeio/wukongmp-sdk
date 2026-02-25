using System.Reflection;
using CSharpModBase;
using ReadyM.Api;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Sdk.GameMode.Coop;

namespace WukongMp.Sdk.Api;

public static class ReadyM
{
    internal static GameplayConfiguration Configuration => DI.Instance.GameplayConfiguration;
    internal static CoopWidgetManager CoopWidgetManager { get; } = new(DI.Instance.WidgetManager, DI.Instance.State, DI.Instance.PlayerState, DI.Instance.EventBus, DI.Instance.FreeCameraManager, DI.Instance.AreaState, DI.Instance.GameplayEventRouter);

    internal static CoopSynchronizer CoopSynchronizer { get; } = new(
        DI.Instance.ArchetypeEvent,
        DI.Instance.State,
        DI.Instance.WukongArchetype,
        DI.Instance.World,
        DI.Instance.MappingPolicyDir,
        DI.Instance.AreaState,
        DI.Instance.PawnState,
        DI.Instance.PlayerState,
        DI.Instance.PlayerPawnState,
        DI.Instance.ModeManager,
        DI.Instance.NetEntity,
        DI.Instance.ClientOwnership_,
        DI.Instance.MappedEvent,
        DI.Instance.JobRegistry,
        DI.Instance.NetComponentRegistry,
        DI.Instance.RelayClient,
        DI.Instance.EcsLoop,
        DI.Instance.EventBus,
        DI.Instance.WidgetManager,
        DI.Instance.GameplayEventRouter,
        DI.Instance.GameplayConfiguration,
        DI.Instance.FreeCameraManager,
        DI.Instance.FreeCameraController,
        DI.Instance.Logger);

    public static WukongClientApi Client { get; } = new(DI.Instance.WukongArchetype, DI.Instance.World, DI.Instance.State, DI.Instance.AreaState, DI.Instance.PlayerState, DI.Instance.PawnState, DI.Instance.ClientNetEntity, DI.Instance.RelayClient, DI.Instance.SaveRelay, DI.Instance.MappedEvent);
    public static WukongLocalApi Local { get; } = new(
        DI.Instance.EventBus,
        DI.Instance.WidgetManager,
        DI.Instance.CommandConsole,
        DI.Instance.GameplayEventRouter,
        DI.Instance.CommandRegistry,
        DI.Instance.EcsLoop);
    
    public static PatcherBase GetPatcher(ModBase mod)
    {
        return new WukongPatcher(mod.GetType().Assembly, mod.Name, DI.Instance.Prelude);
    }
    
    public static PatcherBase GetPatcher(Assembly assembly, string name)
    {
        return new WukongPatcher(assembly, name, DI.Instance.Prelude);
    }
}