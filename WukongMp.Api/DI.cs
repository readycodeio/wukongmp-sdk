using System;
using System.Collections.Generic;
using System.Linq;
using b1;
using BtlShare;
using CSharpModBase;
using DryIoc;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using PreludeLib.Compat;
using PreludeLib.Runtime.Backend;
using PreludeLib.Runtime.Backend.WeaverCallback;
using PreludeLib.Runtime.Public;
using ReadyM.Api.Command;
using ReadyM.Api.Command.Converters;
using ReadyM.Api.DI;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping;
using ReadyM.Api.Mapping.Data;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Mapping.Policies.Data;
using ReadyM.Api.Mapping.Policies.Event;
using ReadyM.Api.Mapping.Policies.Event.Common;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Archetypes;
using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Systems;
using ReadyM.Api.Multiplayer.RPC;
using ReadyM.Api.Multiplayer.Serialization;
using ReadyM.Api.State;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.Mapping.Policies;
using ReadyM.Relay.Client.Serialization;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Client.Utilities;
using ReadyM.Wukong.Common.ECS.Components;
using ReadyM.Wukong.Common.ECS.Registry;
using ReadyM.Wukong.Common.ECS.Values;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Chat;
using WukongMp.Api.Command;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.Input;
using WukongMp.Api.Mapping;
using WukongMp.Api.Mapping.Policies.Event;
using WukongMp.Api.Serialization;
using WukongMp.Api.State;
using WukongMp.Api.Tests;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;
using EquipPosition = ReadyM.Wukong.Common.ECS.Values.EquipPosition;

namespace WukongMp.Api;

internal sealed class DI : IDependencyContainer
{
    public static DI Instance { get; } = new();

    // TODO: Dispose it on game lifetime end.
    public IContainer Container { get; private set; } = new Container(rules =>
        rules.With(FactoryMethod.ConstructorWithResolvableArguments)
            .WithDefaultReuse(Reuse.Singleton)
            .WithUseInterpretation());

    public void RegisterSingleton<TService, TImplementation>(TImplementation instance) where TImplementation : TService
        => Container.RegisterInstance<TService>(instance);

    public T Resolve<T>() => Container.Resolve<T>();
    public IEnumerable<T> ResolveAll<T>() => Container.ResolveMany<T>();

    public void RegisterSingleton<T>() => Container.Register<T>(ifAlreadyRegistered: IfAlreadyRegistered.Replace);
    public void RegisterSingleton<T>(T instance) => Container.RegisterInstance(instance, ifAlreadyRegistered: IfAlreadyRegistered.Replace);

    public void RegisterSingleton<TService>(Type type) => Container.Register(typeof(TService), type, ifAlreadyRegistered: IfAlreadyRegistered.Replace);

    public void RegisterSingleton<TService, TImplementation>() where TImplementation : TService
        => Container.Register<TService, TImplementation>(ifAlreadyRegistered: IfAlreadyRegistered.Replace);

    public InputManager InputManager => Container.Resolve<InputManager>();
    public ILoggerFactory LoggerFactory => Container.Resolve<ILoggerFactory>();
    public ILogger Logger => Container.Resolve<ILogger>();
    public NetworkSessionStats NetworkSessionStats => Container.Resolve<NetworkSessionStats>();

    public Store World => Container.Resolve<Store>();
    public ReceiveSystem Scheduler => Container.Resolve<ReceiveSystem>();
    public ClientEcsUpdateLoop EcsLoop => Container.Resolve<ClientEcsUpdateLoop>();

    public WukongMappingPolicyDirectory MappingPolicyDir => Container.Resolve<WukongMappingPolicyDirectory>();
    public IMappedEntityManager<AActor> MappedEntity => Container.Resolve<IMappedEntityManager<AActor>>();
    public IMappedEventManager MappedEvent => Container.Resolve<IMappedEventManager>();
    public IComponentFieldMappingRegistry MappedField => Container.Resolve<IComponentFieldMappingRegistry>();

    public HotSwappableRelayClient RelayClient => Container.Resolve<HotSwappableRelayClient>();

    public ClientState State => Container.Resolve<ClientState>();

    public ClientOwnershipManager ClientOwnership => Container.Resolve<ClientOwnershipManager>();

    public WukongAreaState AreaState => Container.Resolve<WukongAreaState>();
    public WukongPlayerState PlayerState => Container.Resolve<WukongPlayerState>();
    public WukongPawnState PawnState => Container.Resolve<WukongPawnState>();
    public WukongPlayerModeManager ModeManager => Container.Resolve<WukongPlayerModeManager>();

    public WukongClientRpcCallbacks ClientRpc => Container.Resolve<WukongClientRpcCallbacks>();
    public WukongServerRpcCallbacks ServerRpc => Container.Resolve<WukongServerRpcCallbacks>();
    public WukongEventBus EventBus => Container.Resolve<WukongEventBus>();
    public GameplayConfiguration GameplayConfiguration => Container.Resolve<GameplayConfiguration>();
    public GameplayEventRouter GameplayEventRouter => Container.Resolve<GameplayEventRouter>();

    public WukongConnectionManager Connection => Container.Resolve<WukongConnectionManager>();
    public FreeCameraManager FreeCameraManager => Container.Resolve<FreeCameraManager>();

    public WukongCommandConsole CommandConsole => Container.Resolve<WukongCommandConsole>();
    public WukongInputManager WukongInputManager => Container.Resolve<WukongInputManager>();
    public RuntimePrelude Prelude => Container.Resolve<RuntimePrelude>();
    public WukongWidgetManager WidgetManager => Container.Resolve<WukongWidgetManager>();

    // public ShimController ShimController => _container.Resolve<ShimController>();
    // public ShimPlaybackRelayClient ShimPlaybackRelayClient => _container.Resolve<ShimPlaybackRelayClient>();
    // public ShimAutoStarter ShimAuto => _container.Resolve<ShimAutoStarter>();
    public TestsRunner TestsRunner => Container.Resolve<TestsRunner>();

    public void InitLogging(ILoggerFactory loggerFactory)
    {
        if (Container.IsRegistered<ILoggerFactory>())
            return;

        Container.RegisterInstance(loggerFactory);
        var loggerFactoryMethod = typeof(LoggerFactory).GetMethod("CreateLogger")!;

        Container.Register(typeof(ILogger<>), made: Made.Of(
            req => loggerFactoryMethod.MakeGenericMethod(req.Parent.ImplementationType),
            ServiceInfo.Of<LoggerFactory>()));
        Container.RegisterInstance(LoggerFactory.CreateLogger("Default"));
    }

    public void Init()
    {
        Logger.LogDebug("Initializing DI...");
        Container.RegisterInstance<IDependencyContainer>(Instance);
        
        Container.Register<RpcOffsetProvider>(serviceKey: OffsetProviderKey.Client);
        Container.Register<RpcOffsetProvider>(serviceKey: OffsetProviderKey.Server);

        Container.RegisterInitializer<object>((obj, s) =>
        {
            if (obj is RpcBase client)
            {
                client.RelayClient = s.Resolve<IRpcClient>();
                client.Serializer = s.Resolve<IRelaySerializer>();
                client.Scheduler = s.Resolve<ReceiveSystem>().Scheduler;
            }
        });

        Container.RegisterInstance(LaunchParameters.Instance);
        Container.RegisterInstance(new NetworkSessionStats(LaunchParameters.Instance.UserGuid.ToString(), LaunchParameters.Instance.Region));
        Container.RegisterInstanceMany(InputManager.Instance);

        Container.Register<IAreaComponentRegistration, WukongAreaRegistration>();
        Container.Register<IAreaComponentRegistry, AreaComponentRegistry>();

        Container.Register<IPlayerComponentRegistration, WukongPlayerRegistration>();
        Container.Register<IPlayerComponentRegistry, PlayerComponentRegistry>();

        // TODO: the ArchetypeId on client and server are only in sync because the order of registration is the same
        // This is fragile and should be fixed
        Container.RegisterMany<DefaultAreaArchetypeRegistration>(nonPublicServiceTypes: true);
        Container.RegisterMany<DefaultPlayerArchetypeRegistration>(nonPublicServiceTypes: true);
        Container.RegisterMany<DefaultCellArchetypeRegistration>(nonPublicServiceTypes: true);
        Container.RegisterMany<ClientWukongArchetypeRegistration>(nonPublicServiceTypes: true);

        Container.Register<INetworkedComponentRegistration, DefaultNetworkedComponentRegistration>();
        Container.Register<INetworkedComponentRegistration, WukongNetworkedComponentRegistration>();
        Container.Register<INetworkedComponentRegistry, NetworkedComponentRegistry>();

        // TODO | WTF? - using Register<>, which does the same thing, but lazily,
        // TODO | causes the game to crash with a NullReferenceException in completely unrelated game code
        Container.RegisterInstance(new EntityStore());
        Container.Register<Store>();

        Container.Register<ArchetypeEventRouter>();

        Container.Register<IRelaySerializerRegistration, DefaultRelaySerializerRegistration>();
        Container.Register<IRelaySerializerRegistration, WukongSerializerRegistration>();
        Container.Register<IRelaySerializer, RelaySerializer>();

        Container.RegisterMany<HotSwappableRelayClient>(nonPublicServiceTypes: true);

        Container.Register<INetworkedEntityManager, NetworkedEntityManager>();
        Container.Register<WukongEventBus>();
        Container.Register<GameplayConfiguration>();
        Container.Register<GameplayEventRouter>();

        Container.Register<ITextRelaySerializerRegistration, DefaultTextRelaySerializerRegistration>();
        Container.Register<ITextRelaySerializerRegistration, WukongTextSerializerRegistration>();
        Container.Register<ITextRelaySerializerRegistration, ClientShimTextSerializerRegistration>();
        Container.Register<TextRelaySerializer>();

        Container.Register<ReceiveSystem>();
        Container.Register<ClientEcsUpdateLoop>();
        Container.Register<SerializationJobRegistry>();
        Container.Register<ClientState>();
        Container.Register<WukongPlayerState>();
        Container.Register<IClientEntityManager, ClientNetworkedEntityState>();

        Container.Register<FreeCameraManager>();
        Container.Register<WukongWidgetManager>();
        Container.Register<FreeCameraController>();
        Container.Register<IMappedEntityManager<AActor>, MappedEntityManager<AActor>>();
        Container.Register<WukongPawnState>();
        Container.Register<WukongPlayerPawnState>();
        Container.Register<NetworkedOwnershipManager>();
        Container.Register<ClientOwnershipManager>();
        Container.Register<WukongSynchronizer>();
        Container.Register<CutsceneStatusSynchronizer>();
        Container.Register<WukongAreaState>();
        Container.Register<WukongPlayerModeManager>();
        Container.Register<WukongConnectionManager>();
        Container.Register<WukongNetworkLogger>();
        Container.Register<DataSideChannel>();

        Container.Register<IMappingDataPolicyFactory, OwnershipDataPolicyFactory>();
        Container.Register<IMappingEventPolicyFactory, OwnershipEventPolicyFactory>();
        Container.Register<IMappingEventPolicyFactory, MasterClientEventPolicyFactory>();
        Container.Register<IMappingEventPolicyFactory, RunOnMasterClientOnlyEventPolicyFactory>();
        Container.Register<IMappingEventPolicyFactory, SpawnSummonEventEventPolicyFactory>();
        Container.Register<IMappingEventPolicyFactory, AlwaysPropagatesEventPolicyFactory>();

        Container.RegisterMany<MappingPolicyDirectory>(serviceTypeCondition: type => type.IsInterface, nonPublicServiceTypes: true);
        Container.RegisterInitializer<IMappingPolicyDirectory>((iface, s) =>
        {
            var mapping = (MappingPolicyDirectory)iface;

            var ownership = s.Resolve<ClientOwnershipManager>();
            var area = s.Resolve<WukongAreaState>();

            mapping.RegisterDefaultCreateDelete<AActor>(
                _ => area.IsMasterClient,
                ownership.OwnsEntity);

            foreach (var factory in s.ResolveMany<IMappingDataPolicyFactory>())
            {
                mapping.RegisterDefaultData(factory);
            }

            foreach (var factory in s.ResolveMany<IMappingEventPolicyFactory>())
            {
                mapping.RegisterDefaultEvent(factory);
            }
        });

        Container.Register<IMappedEventManager, MappedEventManager>();
        Container.Register<WukongMappingPolicyDirectory>();
        Container.RegisterMany<ComponentFieldMappingRegistry>(serviceTypeCondition: type => type.IsInterface, nonPublicServiceTypes: true);
        Container.RegisterInitializer<IComponentFieldMappingRegistry>((iface, _) => { RegisterDataMappings((ComponentFieldMappingRegistry)iface); });

        Container.Register<WukongClientGameEvents>();
        Container.Register<WukongClientRpcCallbacks>();

        Container.Register<WukongServerGameEvents>();
        Container.Register<WukongServerRpcCallbacks>();

        Container.Register<WukongChatter>();

        Container.Register<IConsoleCommandRegistration, CheatCommandRegistration>();
        Container.Register<IConsoleCommandRegistration, ConnectionCommandRegistration>();
        Container.Register<IConsoleCommandRegistration, ExecuteWukongCommandRegistration>();
        Container.Register<IConsoleCommandRegistration, GiveUpCommandRegistration>();
        Container.Register<IConsoleCommandRegistration, HelpCommandRegistration>();
        Container.Register<IConsoleCommandRegistration, ObstacleCommandRegistration>();
        Container.Register<IConsoleCommandRegistration, RebirthCommandRegistration>();
        Container.Register<IConsoleCommandRegistration, WorkaroundCommandRegistration>();
        Container.Register<ConsoleCommandRegistry>();

        Container.Register<IConsoleArgumentParserRegistration, StandardArgumentParserRegistration>();
        Container.Register<ConsoleCommandParser>();

        Container.Register<IConsoleArgumentTypeConversion, IdentToStringTypeConversion>();
        Container.Register<IConsoleArgumentTypeConversion, DecimalToIntTypeConversion>();
        Container.Register<IConsoleArgumentTypeConversion, DecimalToFloatTypeConversion>();
        Container.Register<IConsoleArgumentTypeConversion, DecimalToDoubleTypeConversion>();
        Container.Register<ConsoleArgumentTypeConverter>();

        Container.Register<ConsoleCommandMatcher>();
        Container.Register<WukongCommandConsole>();

        Container.Register<WukongInputManager>();
        Container.Register<WukongLevelTransitionConnectionController>();

        Container.Register<NetworkPingMonitor>();
        Container.Register<PingWidgetUpdater>();
        Container.Register<IRuntimeBackend, RuntimeWeaverBackend>();
        Container.Register<RuntimePrelude>();

        Container.Register<WukongSystemRegistration>();
        Container.Register<TestsRunner>();

        Logger.LogDebug("DI Initialized");
#if SHIMMING // TODO: restore shimming functionality
        // ---

        var shimLogger = LoggerFactory.CreateLogger("Shim");
        var shimRecorderLogger = LoggerFactory.CreateLogger("Shim Recorder");
        var shimPlaybackLogger = LoggerFactory.CreateLogger("Shim Playback");

        var shimWorld = new Store(new EntityStore(), _container.ResolveMany<IArchetypeRegistration>());
        var shimRecorderRelayClient = ShimRecorderRelayClient = new HotSwappableRelayClient();
        var shimRecorderRelayService = ShimRelayClientService = new RelayClientService(shimRecorderRelayClient, shimRecorderLogger);
        var shimNetEntity = ShimNetEntity = new NetworkedEntityManager(shimWorld, shimRecorderRelayClient, shimRecorderLogger);
        var shimBlobClient = ShimRelayBlobClient = new HttpBlobClient(shimRecorderLogger);

        var shimEcsLoop = ShimEcsLoop = new ClientEcsUpdateLoop(shimWorld, shimRecorderLogger);
        // var shimState = new ClientState(
        //     shimWorld,
        //     shimNetEntity,
        //     shimRecorderRelayClient,
        //     shimEcsLoop,
        //     jobRegistry,
        //     areaArchetype,
        //     playerArchetype,
        //     shimRecorderLogger
        // );
        //
        // var shimSynchronizer = new ClientNetworkedStateSynchronizer(
        //     shimNetEntity,
        //     shimState,
        //     jobRegistry,
        //     netComponentRegistry,
        //     shimRecorderRelayClient,
        //     shimEcsLoop,
        //     clientOwnership,
        //     shimRecorderLogger
        // );

        var shimParser = ShimParser = new ShimRelayMessageParser([
            new BlobClientShimParserImpl(),
            new ClientSynchronizerShimParserImpl(shimNetEntity, shimLogger),
        ]);
        var shimDepTracker = ShimDepTracker = new ShimReplayDependencyTracker([
            new BlobClientShimTrackerImpl(),
            new ClientSynchronizerShimTrackerImpl(),
        ]);

        var shimPlaybackRelayClient = ShimPlaybackRelayClient = new ShimPlaybackRelayClient(
            shimDepTracker,
            shimParser,
            shimPlaybackLogger
        );

        var shimRecorder = ShimRecorder = new ShimRelayRecorder(shimRecorderRelayClient, shimParser, shimRecorderLogger);
        var shimController = ShimController = new ShimController(shimRecorder, textSerializer, shimRecorderLogger);

        // ---

        var shimAuto = ShimAuto = new ShimAutoStarter(
            state,
            eventBus,
            ecsLoop,
            shimEcsLoop,
            shimPlaybackRelayClient,
            shimRecorder,
            shimBlobClient,
            shimRecorderRelayService,
            shimLogger
        );

#endif
    }

    public void StartHostedServices()
    {
        Container = Container.WithNoMoreRegistrationAllowed();

        var hostedServices = Container.GetServiceRegistrations()
            .Where(r => typeof(IHostedService).IsAssignableFrom(r.Factory.ImplementationType ?? r.ServiceType))
            .Where(r => r.Factory.Reuse is null or SingletonReuse)
            .OrderBy(r => r.FactoryRegistrationOrder)
            .GroupBy(r => r.FactoryRegistrationOrder, (_, r) => r.First());

        foreach (var r in hostedServices)
        {
            var service = (IHostedService)Container.Resolve(r.ServiceType, r.OptionalServiceKey);
            service.OnScopeStart();
            Logger.LogDebug("Started hosted service: {ServiceType}", r.ServiceType.FullName);
        }
    }

    private void RegisterDataMappings(ComponentFieldMappingRegistry fieldMappingRegistry)
    {
        fieldMappingRegistry.Register(MainCharacterComponent.Fields.Velocity.In<BUC_ABPCharacterData>(),
            (ctx, vec) =>
            {
                ctx.Velocity = vec.ToFVector();

                if (ctx.Velocity.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                {
                    ctx.Velocity = FVector.ZeroVector;
                    // vec = FVector.ZeroVector.ToVector3(); // TODO: is this needed?
                }
            }, ctx => ctx.Velocity.ToVector3());

        fieldMappingRegistry.Register(MainCharacterComponent.Fields.MoveAcceleration.In<BUC_ABPCharacterData>(),
            (ctx, vec) =>
            {
                ctx.MoveAcceleration = vec.ToFVector();

                if (ctx.MoveAcceleration.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                {
                    ctx.MoveAcceleration = FVector.ZeroVector;
                    // vec = FVector.ZeroVector.ToVector3(); // TODO: is this needed?
                }
            }, ctx => ctx.MoveAcceleration.ToVector3());

        fieldMappingRegistry.Register(AnimationComponent.Fields.Velocity.In<BUC_ABPCharacterData>(),
            (ctx, vec) =>
            {
                ctx.Velocity = vec.ToFVector();

                if (ctx.Velocity.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                {
                    ctx.Velocity = FVector.ZeroVector;
                    // vec = FVector.ZeroVector.ToVector3(); // TODO: is this needed?
                }
            }, ctx => ctx.Velocity.ToVector3());

        fieldMappingRegistry.Register(AnimationComponent.Fields.MoveAcceleration.In<BUC_ABPCharacterData>(),
            (ctx, vec) =>
            {
                ctx.MoveAcceleration = vec.ToFVector();

                if (ctx.MoveAcceleration.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                {
                    ctx.MoveAcceleration = FVector.ZeroVector;
                    // vec = FVector.ZeroVector.ToVector3(); // TODO: is this needed?
                }
            }, ctx => ctx.MoveAcceleration.ToVector3());


        fieldMappingRegistry.Register(MainCharacterComponent.Fields.Attributes.In<BUC_AttrContainer>(),
            (ctx, attrs) =>
            {
                foreach (var (attr, value) in attrs)
                {
                    ctx.SetFloatValue((EBGUAttrFloat)attr, value);
                }
            }, (ref main, ctx) =>
            {
                var attrs = main.Attributes.ToDictionary();

                foreach (var attr in Constants.SyncedAttributes)
                {
                    var value = ctx.GetFloatValue(attr);
                    attrs[(byte)attr] = value;
                }

                main.Attributes = new AttributesState(attrs);
            });

        fieldMappingRegistry.Register(MainCharacterComponent.Fields.Attributes.In<(EBGUAttrFloat Attr, BUC_AttrContainer Container)>(),
            (ctx, attrs) =>
            {
                if (!Constants.SyncedAttributes.Contains(ctx.Attr))
                    return;

                ctx.Container.SetFloatValue(ctx.Attr, attrs.GetAttribute((byte)ctx.Attr));
            }, (ref main, ctx) =>
            {
                if (!Constants.SyncedAttributes.Contains(ctx.Attr))
                    return;

                var value = ctx.Container.GetFloatValue(ctx.Attr);
                main.Attributes = main.Attributes.WithSetAttribute((byte)ctx.Attr, value);

                // Some attributes have derivatives (computed properties, if you will)
                var calc = AttrMgr<EBGUAttrFloat, float>.getInstance().GetCalc(ctx.Attr, out var valid);
                if (valid)
                {
                    var finalVal = ctx.Container.GetFloatValue(calc.finalVal);
                    main.Attributes = main.Attributes.WithSetAttribute((byte)calc.finalVal, finalVal);
                }
            });

        fieldMappingRegistry.Register(HpComponent.Fields.Hp.In<BUC_AttrContainer>(),
            (ctx, value) =>
            {
                if (value <= -80000)
                {
                    Logging.LogError("Would set HP to {HP} but will not (OOB fall damage)", value);
                    return;
                }

                if (!value.Equals(ctx.GetFloatValue(EBGUAttrFloat.Hp), Constants.FloatComparisonTolerance))
                {
                    ctx.SetFloatValue(EBGUAttrFloat.Hp, value);
                }
            },
            (ref hp, ctx) =>
            {
                hp.Hp = ctx.GetFloatValue(EBGUAttrFloat.Hp);
                if (hp.Hp > 0)
                {
                    hp.IsDead = false;
                }
            });

        fieldMappingRegistry.Register(HpComponent.Fields.HpMaxBase.In<BUC_AttrContainer>(),
            (ctx, value) =>
            {
                if (!value.Equals(ctx.GetFloatValue(EBGUAttrFloat.HpMaxBase), Constants.FloatComparisonTolerance))
                {
                    Logging.LogDebug("Setting HpMaxBase to {HpMaxBase}", value);
                    ctx.SetFloatValue(EBGUAttrFloat.HpMaxBase, value);
                }
            },
            ctx =>
            {
                var value = ctx.GetFloatValue(EBGUAttrFloat.HpMaxBase);
                Logging.LogDebug("Loaded HpMaxBase as {HpMaxBase}", value);
                return value;
            });

        fieldMappingRegistry.Register(MainCharacterComponent.Fields.Equipment.In<BGUCharacterCS>(),
            EquipmentUtils.SetActorEquipment,
            EquipmentUtils.GetCurrentEquipmentStateForActor);

        fieldMappingRegistry.Register(MainCharacterComponent.Fields.Equipment.In<(BGUCharacterCS Pawn, EquipPosition Position)>(),
            (ctx, state) =>
            {
                var item = state.GetItem(ctx.Position);
                EquipmentUtils.SetActorEquipment(ctx.Pawn, ctx.Position, item);
            },
            (ref comp, ctx) =>
            {
                var pawnEq = EquipmentUtils.GetCurrentEquipmentStateForActor(ctx.Pawn);
                var item = pawnEq.GetItem(ctx.Position);
                comp.Equipment = comp.Equipment.WithSetItem(ctx.Position, item);
            });
    }
}