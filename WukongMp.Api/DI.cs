using System.Collections;
using System.Collections.Generic;
using b1;
using BtlB1;
using BtlShare;
using CSharpModBase;
using DryIoc;
using Friflo.Engine.ECS;
using GurCalliopeState;
using Microsoft.Extensions.Logging;
using PreludeLib.Compat;
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
using ReadyM.Relay.Client.Shim.ECS;
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
    private readonly Container _container = new(rules => rules.With(FactoryMethod.ConstructorWithResolvableArguments));

    internal static DI Instance { get; } = new();

    internal InputManager InputManager => _container.Resolve<InputManager>();
    internal ILoggerFactory LoggerFactory => _container.Resolve<ILoggerFactory>();
    internal ILogger Logger => _container.Resolve<ILogger>();
    internal NetworkSessionStats NetworkSessionStats => _container.Resolve<NetworkSessionStats>();

    internal Store World => _container.Resolve<Store>();
    internal ClientWukongArchetypeRegistration WukongArchetype => _container.Resolve<ClientWukongArchetypeRegistration>();
    internal ArchetypeEventRouter ArchetypeEvent => _container.Resolve<ArchetypeEventRouter>();
    internal IClientEcsUpdateLoop EcsLoop => _container.Resolve<IClientEcsUpdateLoop>();

    internal DataSideChannel DataSideChannel => _container.Resolve<DataSideChannel>();
    internal IMappingPolicyDirectoryRegistration MappingPolicyRegistration => _container.Resolve<IMappingPolicyDirectoryRegistration>();
    internal WukongMappingPolicyDirectory MappingPolicyDir => _container.Resolve<WukongMappingPolicyDirectory>();
    internal MappedEntityManager<AActor> MappedEntity => _container.Resolve<MappedEntityManager<AActor>>();
    internal MappedEventManager MappedEvent => _container.Resolve<MappedEventManager>();
    internal WukongClientGameEvents ClientGameEvents => _container.Resolve<WukongClientGameEvents>();
    internal WukongServerGameEvents ServerGameEvents => _container.Resolve<WukongServerGameEvents>();
    internal IComponentFieldMappingRegistry MappedField => _container.Resolve<IComponentFieldMappingRegistry>();

    internal RelaySerializer Serializer => _container.Resolve<RelaySerializer>();
    internal HotSwappableRelayClient RelayClient => _container.Resolve<HotSwappableRelayClient>();
    internal IBlobClient BlobClient => _container.Resolve<IBlobClient>();
    internal NetworkedEntityManager NetEntity => _container.Resolve<NetworkedEntityManager>();
    internal RelayClientService RelayClientService => _container.Resolve<RelayClientService>();

    internal ClientState State => _container.Resolve<ClientState>();
    internal ClientNetworkedEntityManager ClientNetEntity => _container.Resolve<ClientNetworkedEntityManager>();

    internal TextRelaySerializer TextSerializer => _container.Resolve<TextRelaySerializer>();

    internal AreaComponentRegistry AreaComponentRegistry => _container.Resolve<AreaComponentRegistry>();
    internal PlayerComponentRegistry PlayerComponentRegistry => _container.Resolve<PlayerComponentRegistry>();
    internal NetworkedOwnershipManager OwnershipManager_ => _container.Resolve<NetworkedOwnershipManager>();
    internal ClientOwnershipManager ClientOwnership_ => _container.Resolve<ClientOwnershipManager>();

    internal WukongAreaState AreaState => _container.Resolve<WukongAreaState>();
    internal WukongPlayerState PlayerState => _container.Resolve<WukongPlayerState>();
    internal WukongPawnState PawnState => _container.Resolve<WukongPawnState>();
    internal WukongPlayerModeManager ModeManager => _container.Resolve<WukongPlayerModeManager>();
    internal WukongPlayerPawnState PlayerPawnState => _container.Resolve<WukongPlayerPawnState>();

    internal WukongClientRpcCallbacks ClientRpc => _container.Resolve<WukongClientRpcCallbacks>();
    internal WukongServerRpcCallbacks ServerRpc => _container.Resolve<WukongServerRpcCallbacks>();
    internal WukongSaveRelay SaveRelay => _container.Resolve<WukongSaveRelay>();
    internal WukongEventBus EventBus => _container.Resolve<WukongEventBus>();
    internal GameplayConfiguration GameplayConfiguration => _container.Resolve<GameplayConfiguration>();
    internal GameplayEventRouter GameplayEventRouter => _container.Resolve<GameplayEventRouter>();
    internal WukongSynchronizer Synchronizer => _container.Resolve<WukongSynchronizer>();
    internal WukongSystemRegistration SystemRegistration => _container.Resolve<WukongSystemRegistration>();

    internal WukongNetworkLogger NetLogger => _container.Resolve<WukongNetworkLogger>();
    internal INetworkedComponentRegistry NetComponentRegistry => _container.Resolve<INetworkedComponentRegistry>();
    internal JobRegistry JobRegistry => _container.Resolve<JobRegistry>();
    internal WukongConnectionManager Connection => _container.Resolve<WukongConnectionManager>();
    internal WukongLevelTransitionConnectionController ConnectionController => _container.Resolve<WukongLevelTransitionConnectionController>();
    internal NetworkPingMonitor PingMonitor => _container.Resolve<NetworkPingMonitor>();
    internal PingWidgetUpdater PingWidgetUpdater => _container.Resolve<PingWidgetUpdater>();
    internal FreeCameraManager FreeCameraManager => _container.Resolve<FreeCameraManager>();
    internal FreeCameraController FreeCameraController => _container.Resolve<FreeCameraController>();
    internal GameStateSynchronizer GameStateSynchronizer => _container.Resolve<GameStateSynchronizer>();

    internal ConsoleCommandRegistry CommandRegistry => _container.Resolve<ConsoleCommandRegistry>();
    internal ConsoleCommandParser CommandParser => _container.Resolve<ConsoleCommandParser>();
    internal ConsoleArgumentTypeConverter ArgConverter => _container.Resolve<ConsoleArgumentTypeConverter>();
    internal ConsoleCommandMatcher CommandMatcher => _container.Resolve<ConsoleCommandMatcher>();
    internal WukongCommandConsole CommandConsole => _container.Resolve<WukongCommandConsole>();
    internal WukongChatter Chatter => _container.Resolve<WukongChatter>();
    internal WukongInputManager WukongInputManager => _container.Resolve<WukongInputManager>();

    internal RuntimePrelude Prelude => _container.Resolve<RuntimePrelude>();
    internal RuntimeWeaverBackend PreludeBackend => _container.Resolve<RuntimeWeaverBackend>();

    internal WukongWidgetManager WidgetManager => _container.Resolve<WukongWidgetManager>();
    internal TimerController TimerController => _container.Resolve<TimerController>();

    internal ShimRelayMessageParser ShimParser => _container.Resolve<ShimRelayMessageParser>();
    internal ShimReplayDependencyTracker ShimDepTracker => _container.Resolve<ShimReplayDependencyTracker>();
    internal ShimReplayDependencyTracker ShimReplayDependencyTracker => _container.Resolve<ShimReplayDependencyTracker>();
    internal HotSwappableRelayClient ShimRecorderRelayClient => _container.Resolve<HotSwappableRelayClient>();
    internal ShimRelayRecorder ShimRecorder => _container.Resolve<ShimRelayRecorder>();
    internal ShimController ShimController => _container.Resolve<ShimController>();
    internal ShimPlaybackRelayClient ShimPlaybackRelayClient => _container.Resolve<ShimPlaybackRelayClient>();
    internal ClientEcsUpdateLoop ShimEcsLoop => _container.Resolve<ClientEcsUpdateLoop>();
    internal RelayClientService ShimRelayClientService => _container.Resolve<RelayClientService>();
    internal NetworkedEntityManager ShimNetEntity => _container.Resolve<NetworkedEntityManager>();
    internal HttpBlobClient ShimRelayBlobClient => _container.Resolve<HttpBlobClient>();

    internal ShimAutoStarter ShimAuto => _container.Resolve<ShimAutoStarter>();

    internal TestsRunner TestsRunner => _container.Resolve<TestsRunner>();

    internal void InitLogging(ILoggerFactory loggerFactory)
    {
        _container.RegisterInstance(loggerFactory);
        _container.RegisterInstance(LoggerFactory.CreateLogger(""));
    }

    internal void Init()
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
        
        _container.RegisterInitializer<MappingPolicyDirectory>((mapping, s) =>
        {
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
        
        var policyDir = new MappingPolicyDirectory(sideChannel);
        policyDir.RegisterDefaultCreateDelete<AActor>(
            actor => areaState.IsMasterClient,
            entity => clientOwnership.OwnsEntity(entity));
        policyDir.RegisterDefaultData(new OwnershipDataPolicyFactory(clientOwnership, sideChannel));
        policyDir.RegisterDefaultEvent(new OwnershipEventPolicyFactory(clientOwnership, sideChannel));
        policyDir.RegisterDefaultEvent(new MasterClientEventPolicyFactory(areaState, sideChannel));
        policyDir.RegisterDefaultEvent(new RunOnMasterClientOnlyEventPolicyFactory(clientOwnership, areaState, sideChannel));
        policyDir.RegisterDefaultEvent(new SpawnSummonEventEventPolicyFactory(clientOwnership, playerState, areaState, world, sideChannel));
        policyDir.RegisterDefaultEvent(new AlwaysPropagatesEventPolicyFactory(sideChannel));
        MappingPolicyRegistration = policyDir;

        var mappedEvent = MappedEvent = new MappedEventManager(sideChannel, policyDir, logger);
        var mappingPolicyDir = MappingPolicyDir = new WukongMappingPolicyDirectory(policyDir, mappedEntity, mappedEvent, wukongArchetype);

        var mappedField = new ComponentFieldMappingRegistry(policyDir, sideChannel, logger);
        MappedField = mappedField;
        RegisterDataMappings(mappedField);

        var saveRelay = SaveRelay = new WukongSaveRelay(blobClient, logger);
        var clientGameEvents = ClientGameEvents = new WukongClientGameEvents(mappedEvent, mappingPolicyDir, state, pawnState, playerState, widgetManager, gameplayEventRouter, logger);
        var serverGameEvents = ServerGameEvents = new WukongServerGameEvents(mappedEvent, widgetManager, logger);

        var clientRpc = ClientRpc = new WukongClientRpcCallbacks(ecsLoop, playerState, areaState, mappedEvent, mappingPolicyDir, serializer, relayClient, clientNetEntity, widgetManager, timerController, logger);
        var serverRpc = ServerRpc = new WukongServerRpcCallbacks(ecsLoop, mappedEvent, mappingPolicyDir, relayClient, widgetManager, logger);
        var chatter = Chatter = new WukongChatter(playerState, clientRpc, widgetManager, logger);

        var commandRegistry = CommandRegistry = new ConsoleCommandRegistry([
            new CheatCommandRegistration(playerState, areaState, chatter, serverRpc),
            new ConnectionCommandRegistration(playerState, connection, chatter),
            new ExecuteWukongCommandRegistration(),
            new GiveUpCommandRegistration(ecsLoop, playerState, chatter),
            new ObstacleCommandRegistration(),
            new RebirthCommandRegistration(playerState, mappedEvent, chatter),
            new WorkaroundCommandRegistration(world, mappedEvent, playerState),
        ]);
        var commandParser = CommandParser = new ConsoleCommandParser([
            new StandardArgumentParserRegistration(),
        ]);
        var commandTypeConverter = ArgConverter = new ConsoleArgumentTypeConverter([
            new IdentToStringTypeConversion(),
            new DecimalToIntTypeConversion(),
            new DecimalToFloatTypeConversion(),
            new DecimalToDoubleTypeConversion(),
        ]);
        var commandMatcher = CommandMatcher = new ConsoleCommandMatcher(commandParser, commandRegistry, commandTypeConverter);
        var commandConsole = CommandConsole = new WukongCommandConsole(commandMatcher, areaState, playerState, eventBus, chatter, widgetManager);
        var wukongInputManager = WukongInputManager = new WukongInputManager(commandConsole, chatter, widgetManager);

        var connectionController = ConnectionController = new WukongLevelTransitionConnectionController(eventBus, connection);

        var pingMonitor = PingMonitor = new NetworkPingMonitor(relayClient);
        var pingWidgetUpdater = PingWidgetUpdater = new PingWidgetUpdater(pingMonitor, serverRpc);

        var runtimeLogger = LoggerFactory.CreateLogger("Runtime");
        var preludeBackend = PreludeBackend = new RuntimeWeaverBackend(runtimeLogger);
        var prelude = Prelude = new RuntimePrelude(preludeBackend, runtimeLogger);

        var systemRegistration = SystemRegistration = new WukongSystemRegistration(
            worldEvent,
            state,
            wukongArchetype,
            mappedField,
            areaState,
            playerState,
            playerPawnState,
            modeManager,
            clientOwnership,
            ecsLoop,
            mappedEvent,
            eventBus,
            widgetManager,
            gameplayEventRouter,
            gameplayConfig,
            freeCameraManager,
            freeCameraController,
            logger
        );
        // ---

        var shimLogger = LoggerFactory.CreateLogger("Shim");
        var shimRecorderLogger = LoggerFactory.CreateLogger("Shim Recorder");
        var shimPlaybackLogger = LoggerFactory.CreateLogger("Shim Playback");

        var shimWorld = new Store(new EntityStore(), [
            areaArchetype,
            playerArchetype,
            wukongArchetype,
        ]);
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

        // ---

        TestsRunner = new TestsRunner(logger);

        // ---

        Logger.LogDebug("DI Initialized");
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