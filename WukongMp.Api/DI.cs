using b1;
using BtlShare;
using CSharpModBase;
using DryIoc;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using PreludeLib.Runtime.Backend.WeaverCallback;
using PreludeLib.Runtime.Public;
using ReadyM.Api.Command;
using ReadyM.Api.Command.Converters;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Helpers;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Archetypes;
using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Mapping;
using ReadyM.Api.Multiplayer.Mapping.Data;
using ReadyM.Api.Multiplayer.Mapping.Events;
using ReadyM.Api.Multiplayer.Mapping.Policies.Data;
using ReadyM.Api.Multiplayer.Mapping.Policies.Event;
using ReadyM.Api.Multiplayer.Mapping.Policies.Event.Common;
using ReadyM.Api.Multiplayer.Serialization;
using ReadyM.Api.State;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.Host;
using ReadyM.Relay.Client.Mapping.Policies;
using ReadyM.Relay.Client.Serialization;
using ReadyM.Relay.Client.Shim;
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
using WukongMp.Api.ECS.Managers;
using WukongMp.Api.FreeCamera;
using WukongMp.Api.Helpers;
using WukongMp.Api.Https;
using WukongMp.Api.Input;
using WukongMp.Api.Mapping;
using WukongMp.Api.Mapping.Policies.Event;
using WukongMp.Api.Serialization;
using WukongMp.Api.Shim;
using WukongMp.Api.State;
using WukongMp.Api.Tests;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;
using EquipPosition = ReadyM.Wukong.Common.ECS.Values.EquipPosition;

namespace WukongMp.Api;

internal sealed class DI
{
    public static DI Instance { get; } = new();

    private readonly Container _container = new(rules =>
        rules.With(FactoryMethod.ConstructorWithResolvableArguments)
            .WithDefaultReuse(Reuse.Singleton)); // TODO: Dispose it on game lifetime end.

    public T Get<T>() => _container.Resolve<T>();

    public InputManager InputManager => _container.Resolve<InputManager>();
    public ILoggerFactory LoggerFactory => _container.Resolve<ILoggerFactory>();
    public ILogger Logger => _container.Resolve<ILogger>();
    public NetworkSessionStats NetworkSessionStats => _container.Resolve<NetworkSessionStats>();

    public Store World => _container.Resolve<Store>();
    public ClientWukongArchetypeRegistration WukongArchetype => _container.Resolve<ClientWukongArchetypeRegistration>();
    public ArchetypeEventRouter ArchetypeEvent => _container.Resolve<ArchetypeEventRouter>();
    public IClientEcsUpdateLoop EcsLoop => _container.Resolve<IClientEcsUpdateLoop>();

    public WukongMappingPolicyDirectory MappingPolicyDir => _container.Resolve<WukongMappingPolicyDirectory>();
    public MappedEntityManager<AActor> MappedEntity => _container.Resolve<MappedEntityManager<AActor>>();
    public MappedEventManager MappedEvent => _container.Resolve<MappedEventManager>();
    public IComponentFieldMappingRegistry MappedField => _container.Resolve<IComponentFieldMappingRegistry>();

    public RelaySerializer Serializer => _container.Resolve<RelaySerializer>();
    public HotSwappableRelayClient RelayClient => _container.Resolve<HotSwappableRelayClient>();
    public NetworkedEntityManager NetEntity => _container.Resolve<NetworkedEntityManager>();

    public ClientState State => _container.Resolve<ClientState>();
    public ClientNetworkedEntityManager ClientNetEntity => _container.Resolve<ClientNetworkedEntityManager>();

    public TextRelaySerializer TextSerializer => _container.Resolve<TextRelaySerializer>();

    public ClientOwnershipManager ClientOwnership_ => _container.Resolve<ClientOwnershipManager>();

    public WukongAreaState AreaState => _container.Resolve<WukongAreaState>();
    public WukongPlayerState PlayerState => _container.Resolve<WukongPlayerState>();
    public WukongPawnState PawnState => _container.Resolve<WukongPawnState>();
    public WukongPlayerModeManager ModeManager => _container.Resolve<WukongPlayerModeManager>();
    public WukongPlayerPawnState PlayerPawnState => _container.Resolve<WukongPlayerPawnState>();

    public WukongClientRpcCallbacks ClientRpc => _container.Resolve<WukongClientRpcCallbacks>();
    public WukongServerRpcCallbacks ServerRpc => _container.Resolve<WukongServerRpcCallbacks>();
    public WukongSaveRelay SaveRelay => _container.Resolve<WukongSaveRelay>();
    public WukongEventBus EventBus => _container.Resolve<WukongEventBus>();
    public GameplayConfiguration GameplayConfiguration => _container.Resolve<GameplayConfiguration>();
    public GameplayEventRouter GameplayEventRouter => _container.Resolve<GameplayEventRouter>();

    public WukongNetworkLogger NetLogger => _container.Resolve<WukongNetworkLogger>();
    public INetworkedComponentRegistry NetComponentRegistry => _container.Resolve<INetworkedComponentRegistry>();
    public JobRegistry JobRegistry => _container.Resolve<JobRegistry>();
    public WukongConnectionManager Connection => _container.Resolve<WukongConnectionManager>();
    public FreeCameraManager FreeCameraManager => _container.Resolve<FreeCameraManager>();
    public FreeCameraController FreeCameraController => _container.Resolve<FreeCameraController>();

    public ConsoleCommandRegistry CommandRegistry => _container.Resolve<ConsoleCommandRegistry>();
    public WukongCommandConsole CommandConsole => _container.Resolve<WukongCommandConsole>();
    public WukongChatter Chatter => _container.Resolve<WukongChatter>();
    public WukongInputManager WukongInputManager => _container.Resolve<WukongInputManager>();
    public RuntimePrelude Prelude => _container.Resolve<RuntimePrelude>();
    public WukongWidgetManager WidgetManager => _container.Resolve<WukongWidgetManager>();
    public HotSwappableRelayClient ShimRecorderRelayClient => _container.Resolve<HotSwappableRelayClient>();
    public ShimController ShimController => _container.Resolve<ShimController>();
    public ShimPlaybackRelayClient ShimPlaybackRelayClient => _container.Resolve<ShimPlaybackRelayClient>();
    public ShimAutoStarter ShimAuto => _container.Resolve<ShimAutoStarter>();
    public TestsRunner TestsRunner => _container.Resolve<TestsRunner>();

    public void InitLogging(ILoggerFactory loggerFactory)
    {
        _container.RegisterInstance(loggerFactory);
        _container.RegisterInstance(LoggerFactory.CreateLogger(""));
    }

    public void Init()
    {
        Logger.LogDebug("Initializing DI...");

        _container.RegisterInstance(new NetworkSessionStats(LaunchParameters.Instance.UserGuid.ToString(), LaunchParameters.Instance.Region));
        _container.RegisterInstance(InputManager.Instance);

        _container.Register<IAreaComponentRegistration, WukongAreaRegistration>();
        _container.Register<IAreaComponentRegistry, AreaComponentRegistry>();

        _container.Register<IPlayerComponentRegistration, WukongPlayerRegistration>();
        _container.Register<IPlayerComponentRegistry, PlayerComponentRegistry>();

        // TODO: the ArchetypeId on client and server are only in sync because the order of registration is the same
        // This is fragile and should be fixed
        _container.Register<IArchetypeRegistration, DefaultAreaArchetypeRegistration>();
        _container.Register<IArchetypeRegistration, DefaultPlayerArchetypeRegistration>();
        _container.Register<IArchetypeRegistration, ClientWukongArchetypeRegistration>();

        _container.Register<INetworkedComponentRegistration, DefaultNetworkedComponentRegistration>();
        _container.Register<INetworkedComponentRegistration, WukongNetworkedComponentRegistration>();
        _container.Register<INetworkedComponentRegistry, NetworkedComponentRegistry>();

        _container.Register<EntityStore>();
        _container.Register<Store>();

        _container.Register<ArchetypeEventRouter>();

        _container.Register<IRelaySerializerRegistration, DefaultRelaySerializerRegistration>();
        _container.Register<IRelaySerializerRegistration, WukongSerializerRegistration>();
        _container.Register<RelaySerializer>();

        _container.Register<IRelayClient, HotSwappableRelayClient>();

        _container.Register<IBlobClient, HttpBlobClient>();

        _container.Register<NetworkedEntityManager>();
        _container.Register<RelayClientService>();
        _container.Register<WukongEventBus>();
        _container.Register<GameplayConfiguration>();
        _container.Register<GameplayEventRouter>();

        _container.Register<ITextRelaySerializerRegistration, DefaultTextRelaySerializerRegistration>();
        _container.Register<ITextRelaySerializerRegistration, WukongTextSerializerRegistration>();
        _container.Register<ITextRelaySerializerRegistration, ClientShimTextSerializerRegistration>();
        _container.Register<TextRelaySerializer>();

        _container.Register<IClientEcsUpdateLoop, ClientEcsUpdateLoop>();
        _container.Register<JobRegistry>();
        _container.Register<ClientState>();
        _container.Register<WukongPlayerState>();
        _container.Register<IEntityManager, ClientNetworkedEntityManager>();

        _container.Register<FreeCameraManager>();
        _container.Register<WukongWidgetManager>();
        _container.Register<TimerController>();
        _container.Register<FreeCameraController>();
        _container.Register<IMappedEntityManager<AActor>, MappedEntityManager<AActor>>();
        _container.Register<WukongPawnState>();
        _container.Register<WukongPlayerPawnState>();
        _container.Register<NetworkedOwnershipManager>();
        _container.Register<ClientOwnershipManager>();
        _container.Register<WukongSynchronizer>();
        _container.Register<GameStateSynchronizer>();
        _container.Register<WukongAreaState>();
        _container.Register<WukongPlayerModeManager>();
        _container.Register<WukongConnectionManager>();
        _container.Register<WukongNetworkLogger>();
        _container.Register<DataSideChannel>();

        _container.Register<IMappingDataPolicyFactory, OwnershipDataPolicyFactory>();
        _container.Register<IMappingEventPolicyFactory, OwnershipEventPolicyFactory>();
        _container.Register<IMappingEventPolicyFactory, MasterClientEventPolicyFactory>();
        _container.Register<IMappingEventPolicyFactory, RunOnMasterClientOnlyEventPolicyFactory>();
        _container.Register<IMappingEventPolicyFactory, SpawnSummonEventEventPolicyFactory>();
        _container.Register<IMappingEventPolicyFactory, AlwaysPropagatesEventPolicyFactory>();

        _container.RegisterMany<MappingPolicyDirectory>(serviceTypeCondition: type => type.IsInterface);
        _container.RegisterInitializer<IMappingPolicyDirectory>((iface, s) =>
        {
            var mapping = (MappingPolicyDirectory) iface;

            var ownership = s.Resolve<ClientOwnershipManager>();
            var area = s.Resolve<WukongAreaState>();

            mapping.RegisterDefaultCreateDelete<AActor>(
                _ => area.IsMasterClient,
                entity => ownership.OwnsEntity(entity));

            foreach (var factory in s.ResolveMany<IMappingDataPolicyFactory>())
            {
                mapping.RegisterDefaultData(factory);
            }

            foreach (var factory in s.ResolveMany<IMappingEventPolicyFactory>())
            {
                mapping.RegisterDefaultEvent(factory);
            }
        });

        _container.Register<IMappedEventManager, MappedEventManager>();
        _container.Register<WukongMappingPolicyDirectory>();
        _container.RegisterMany<ComponentFieldMappingRegistry>(serviceTypeCondition: type => type.IsInterface);
        _container.RegisterInitializer<IComponentFieldMappingRegistry>((iface, _) => { RegisterDataMappings((ComponentFieldMappingRegistry) iface); });

        _container.Register<IWukongSaveRelay, WukongSaveRelay>();

        _container.Register<WukongClientGameEvents>();
        _container.Register<WukongClientRpcCallbacks>();

        _container.Register<WukongServerGameEvents>();
        _container.Register<WukongServerRpcCallbacks>();

        _container.Register<WukongChatter>();

        _container.Register<IConsoleCommandRegistration, CheatCommandRegistration>();
        _container.Register<IConsoleCommandRegistration, ConnectionCommandRegistration>();
        _container.Register<IConsoleCommandRegistration, ExecuteWukongCommandRegistration>();
        _container.Register<IConsoleCommandRegistration, GiveUpCommandRegistration>();
        _container.Register<IConsoleCommandRegistration, ObstacleCommandRegistration>();
        _container.Register<IConsoleCommandRegistration, RebirthCommandRegistration>();
        _container.Register<IConsoleCommandRegistration, WorkaroundCommandRegistration>();
        _container.Register<ConsoleCommandRegistry>();

        _container.Register<IConsoleArgumentParserRegistration, StandardArgumentParserRegistration>();
        _container.Register<ConsoleCommandParser>();

        _container.Register<IConsoleArgumentTypeConversion, IdentToStringTypeConversion>();
        _container.Register<IConsoleArgumentTypeConversion, DecimalToIntTypeConversion>();
        _container.Register<IConsoleArgumentTypeConversion, DecimalToFloatTypeConversion>();
        _container.Register<IConsoleArgumentTypeConversion, DecimalToDoubleTypeConversion>();
        _container.Register<ConsoleArgumentTypeConverter>();

        _container.Register<ConsoleCommandMatcher>();
        _container.Register<WukongCommandConsole>();

        _container.Register<WukongInputManager>();
        _container.Register<WukongLevelTransitionConnectionController>();

        _container.Register<NetworkPingMonitor>();
        _container.Register<PingWidgetUpdater>();
        _container.Register<RuntimeWeaverBackend>();
        _container.Register<RuntimePrelude>();

        _container.Register<WukongSystemRegistration>();
        _container.Register<TestsRunner>();

        Logger.LogDebug("DI Initialized");
#if SHIMMING
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
                    ctx.SetFloatValue((EBGUAttrFloat) attr, value);
                }
            }, (ref main, ctx) =>
            {
                var attrs = main.Attributes.ToDictionary();

                foreach (var attr in Constants.SyncedAttributes)
                {
                    var value = ctx.GetFloatValue(attr);
                    attrs[(byte) attr] = value;
                }

                main.Attributes = new AttributesState(attrs);
            });

        fieldMappingRegistry.Register(MainCharacterComponent.Fields.Attributes.In<(EBGUAttrFloat Attr, BUC_AttrContainer Container)>(),
            (ctx, attrs) =>
            {
                if (!Constants.SyncedAttributes.Contains(ctx.Attr))
                    return;

                ctx.Container.SetFloatValue(ctx.Attr, attrs.GetAttribute((byte) ctx.Attr));
            }, (ref main, ctx) =>
            {
                if (!Constants.SyncedAttributes.Contains(ctx.Attr))
                    return;

                var value = ctx.Container.GetFloatValue(ctx.Attr);
                main.Attributes = main.Attributes.WithSetAttribute((byte) ctx.Attr, value);

                // Some attributes have derivatives (computed properties, if you will)
                var calc = AttrMgr<EBGUAttrFloat, float>.getInstance().GetCalc(ctx.Attr, out var valid);
                if (valid)
                {
                    var finalVal = ctx.Container.GetFloatValue(calc.finalVal);
                    main.Attributes = main.Attributes.WithSetAttribute((byte) calc.finalVal, finalVal);
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
                if (!value.Equals(ctx.GetFloatValue(EBGUAttrFloat.HpMaxBase),
                        Constants.FloatComparisonTolerance))
                {
                    ctx.SetFloatValue(EBGUAttrFloat.HpMaxBase, value);
                }
            },
            ctx => ctx.GetFloatValue(EBGUAttrFloat.HpMaxBase));

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