using b1;
using BtlShare;
using CSharpModBase;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using PreludeLib.Compat;
using PreludeLib.Runtime.Backend.WeaverCallback;
using PreludeLib.Runtime.Public;
using ReadyM.Api.Command;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping;
using ReadyM.Api.Mapping.Api;
using ReadyM.Api.Mapping.Events;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.Host;
using ReadyM.Relay.Client.Serialization;
using ReadyM.Relay.Client.Shim;
using ReadyM.Relay.Client.Shim.ECS;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Client.Utilities;
using ReadyM.Relay.Common.ECS.Archetypes;
using ReadyM.Relay.Common.ECS.Jobs;
using ReadyM.Relay.Common.ECS.Registry;
using ReadyM.Relay.Common.Serialization;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Registry;
using UnrealEngine.Engine;
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
using WukongMp.Api.Mapping.Data;
using WukongMp.Api.Mapping.Events;
using WukongMp.Api.Serialization;
using WukongMp.Api.Shim;
using WukongMp.Api.State;
using WukongMp.Api.Tests;
using WukongMp.Api.UI;

namespace WukongMp.Api;

internal sealed class DI
{
    internal static DI Instance { get; } = new();

    internal InputManager InputManager { get; private set; } = null!;
    internal ILoggerFactory LoggerFactory { get; private set; } = null!;
    internal ILogger Logger { get; private set; } = null!;
    internal NetworkSessionStats NetworkSessionStats { get; private set; } = null!;

    internal Store World { get; private set; } = null!;
    internal ClientWukongArchetypeRegistration WukongArchetype { get; private set; } = null!;
    internal ArchetypeEventRouter ArchetypeEvent { get; private set; } = null!;
    internal IClientEcsUpdateLoop EcsLoop { get; private set; } = null!;

    internal WukongMappingPolicyDirectory MappingPolicyDir { get; private set; } = null!;
    internal MappedEntityManager<AActor> MappedEntity { get; private set; } = null!;
    internal MappedEventManager MappedEvent { get; private set; } = null!;
    internal WukongClientGameEvents ClientGameEvents { get; private set; } = null!;
    internal WukongServerGameEvents ServerGameEvents { get; private set; } = null!;
    internal IComponentFieldMappingRegistry FieldMappingRegistry { get; private set; } = null!;
    internal StandardDataMappings StandardDataMappings { get; private set; } = null!;

    internal RelaySerializer Serializer { get; private set; } = null!;
    internal HotSwappableRelayClient RelayClient { get; private set; } = null!;
    internal IBlobClient BlobClient { get; set; } = null!;
    internal NetworkedEntityManager NetEntity { get; private set; } = null!;
    internal RelayClientService RelayClientService { get; private set; } = null!;

    internal ClientState State { get; private set; } = null!;
    internal ClientNetworkedEntityManager ClientNetEntity { get; private set; } = null!;

    internal TextRelaySerializer TextSerializer { get; private set; } = null!;

    internal AreaComponentRegistry AreaComponentRegistry { get; private set; } = null!;
    internal PlayerComponentRegistry PlayerComponentRegistry { get; private set; } = null!;
    internal NetworkedOwnershipManager OwnershipManager_ { get; private set; } = null!;
    internal ClientOwnershipManager ClientOwnership_ { get; private set; } = null!;

    internal WukongAreaState AreaState { get; private set; } = null!;
    internal WukongPlayerState PlayerState { get; private set; } = null!;
    internal WukongPawnState PawnState { get; private set; } = null!;
    internal WukongPlayerModeManager ModeManager { get; private set; } = null!;
    internal WukongPlayerPawnState PlayerPawnState { get; private set; } = null!;

    internal WukongClientRpcCallbacks ClientRpc { get; private set; } = null!;
    internal WukongServerRpcCallbacks ServerRpc { get; private set; } = null!;
    internal WukongSaveRelay SaveRelay { get; private set; } = null!;
    internal WukongEventBus EventBus { get; private set; } = null!;
    internal GameplayConfiguration GameplayConfiguration { get; private set; } = null!;
    internal GameplayEventRouter GameplayEventRouter { get; private set; } = null!;

    internal WukongNetworkLogger NetLogger { get; private set; } = null!;
    internal INetworkedComponentRegistry NetComponentRegistry { get; private set; } = null!;
    internal JobRegistry JobRegistry { get; private set; } = null!;
    internal WukongSynchronizer Synchronizer { get; private set; } = null!;
    internal WukongConnectionManager Connection { get; private set; } = null!;
    internal WukongLevelTransitionConnectionController ConnectionController { get; private set; } = null!;
    internal NetworkPingMonitor PingMonitor { get; private set; } = null!;
    internal PingWidgetUpdater PingWidgetUpdater { get; private set; } = null!;
    internal FreeCameraManager FreeCameraManager { get; private set; } = null!;
    internal FreeCameraController FreeCameraController { get; private set; } = null!;
    internal GameStateSynchronizer GameStateSynchronizer { get; private set; } = null!;

    internal ConsoleCommandRegistry CommandRegistry { get; set; } = null!;
    internal ConsoleCommandParser CommandParser { get; set; } = null!;
    internal ConsoleArgumentTypeConverter ArgConverter { get; set; } = null!;
    internal ConsoleCommandMatcher CommandMatcher { get; set; } = null!;
    internal WukongCommandConsole CommandConsole { get; set; } = null!;
    internal WukongChatter Chatter { get; private set; } = null!;
    internal WukongInputManager WukongInputManager { get; private set; } = null!;

    internal RuntimePrelude Prelude { get; private set; } = null!;
    internal RuntimeWeaverBackend PreludeBackend { get; private set; } = null!;

    internal WukongWidgetManager WidgetManager { get; private set; } = null!;
    internal TimerController TimerController { get; private set; } = null!;

    internal ShimRelayMessageParser ShimParser { get; private set; } = null!;
    internal ShimReplayDependencyTracker ShimDepTracker { get; set; } = null!;
    internal ShimReplayDependencyTracker ShimReplayDependencyTracker { get; private set; } = null!;
    internal HotSwappableRelayClient ShimRecorderRelayClient { get; set; } = null!;
    internal ShimRelayRecorder ShimRecorder { get; private set; } = null!;
    internal ShimController ShimController { get; private set; } = null!;
    internal ShimPlaybackRelayClient ShimPlaybackRelayClient { get; private set; } = null!;
    internal ClientEcsUpdateLoop ShimEcsLoop { get; set; } = null!;
    internal RelayClientService ShimRelayClientService { get; set; } = null!;
    internal NetworkedEntityManager ShimNetEntity { get; set; } = null!;
    internal HttpBlobClient ShimRelayBlobClient { get; set; } = null!;

    internal ShimAutoStarter ShimAuto { get; set; } = null!;

    internal TestsRunner TestsRunner { get; set; } = null!;

    internal void InitLogging(ILoggerFactory loggerFactory)
    {
        LoggerFactory = loggerFactory;
        Logger = LoggerFactory.CreateLogger("");
    }

    internal void Init()
    {
        Logger.LogDebug("Initializing DI...");

        var loggerFactory = LoggerFactory;
        var logger = Logger;
        var pingStatistics = NetworkSessionStats = new NetworkSessionStats(LaunchParameters.Instance.UserGuid.ToString(), LaunchParameters.Instance.Region);

        var inputManager = InputManager = InputManager.Instance;

        var areaComponentRegistry = AreaComponentRegistry = new AreaComponentRegistry([
            new WukongAreaRegistration(),
        ]);
        var playerComponentRegistry = PlayerComponentRegistry = new PlayerComponentRegistry([
            new WukongPlayerRegistration(),
        ]);
        var areaArchetype = new DefaultAreaArchetypeRegistration(areaComponentRegistry);
        var playerArchetype = new DefaultPlayerArchetypeRegistration(playerComponentRegistry);
        var wukongArchetype = WukongArchetype = new ClientWukongArchetypeRegistration();

        var netComponentRegistry = NetComponentRegistry = new NetworkedComponentRegistry([
            new DefaultNetworkedComponentRegistration(),
            new WukongNetworkedComponentRegistration(),
        ]);

        // TODO: the ArchetypeId on client and server are only in sync because the order of registration is the same
        // This is fragile and should be fixed
        var world = World = new Store(new EntityStore(), [
            areaArchetype,
            playerArchetype,
            wukongArchetype,
        ]);

        var worldEvent = ArchetypeEvent = new ArchetypeEventRouter(world);
        var serializer = Serializer = new RelaySerializer([
            new DefaultRelaySerializerRegistration(),
            new WukongSerializerRegistration(),
        ]);

        var relayClient = RelayClient = new HotSwappableRelayClient();
        var blobClient = BlobClient = new HttpBlobClient(logger);
        var netEntity = NetEntity = new NetworkedEntityManager(world, relayClient, logger);
        var relayClientService = RelayClientService = new RelayClientService(relayClient, logger);

        var eventBus = EventBus = new WukongEventBus();

        var gameplayConfig = GameplayConfiguration = new GameplayConfiguration(logger);
        var gameplayEventRouter = GameplayEventRouter = new GameplayEventRouter();

        var textSerializer = TextSerializer = new TextRelaySerializer([
            new DefaultTextRelaySerializerRegistration(),
            new WukongTextSerializerRegistration(),
            new ClientShimTextSerializerRegistration(),
        ]);

        var ecsLoop = EcsLoop = new ClientEcsUpdateLoop(world, logger);

        var jobRegistry = JobRegistry = new JobRegistry(netComponentRegistry, netEntity, relayClient, logger);

        var state = State = new ClientState(world, netEntity, relayClient, ecsLoop, jobRegistry, areaArchetype, playerArchetype, logger);
        var clientNetEntity = ClientNetEntity = new ClientNetworkedEntityManager(state, netEntity);
        var playerState = PlayerState = new WukongPlayerState(world, wukongArchetype, clientNetEntity, state, logger);

        var widgetManager = WidgetManager = new WukongWidgetManager(state, playerState, relayClient);
        var timerController = TimerController = new TimerController(widgetManager);

        var freeCameraManager = FreeCameraManager = new FreeCameraManager(playerState);
        var freeCameraController = FreeCameraController = new FreeCameraController(state, playerState, inputManager, freeCameraManager, widgetManager);

        var mappedEntity = MappedEntity = new MappedEntityManager<AActor>(world);
        var pawnState = PawnState = new WukongPawnState(world, mappedEntity, wukongArchetype, clientNetEntity);
        var playerPawnState = PlayerPawnState = new WukongPlayerPawnState(freeCameraManager, world, playerState, logger);

        var ownershipManager = OwnershipManager_ = new NetworkedOwnershipManager(world, logger);
        var clientOwnership = ClientOwnership_ = new ClientOwnershipManager(state, ownershipManager);

        var gameStateSynchronizer = GameStateSynchronizer = new GameStateSynchronizer(state, playerState);

        var areaState = AreaState = new WukongAreaState(state, world, clientOwnership);
        var modeManager = ModeManager = new WukongPlayerModeManager(state, gameplayEventRouter, freeCameraManager);

        var connection = Connection = new WukongConnectionManager(relayClientService, state, playerState, areaState, logger);
        var netLogger = NetLogger = new WukongNetworkLogger(world, state, areaState, playerState, logger);

        var sideChannel = new DataSideChannel();

        var policyDir = new MappingPolicyDirectory();
        policyDir.RegisterDefaultCreateDelete<AActor>(
            actor => areaState.IsMasterClient,
            entity => clientOwnership.OwnsEntity(entity));
        policyDir.RegisterDefaultData(new OwnershipDataPolicyFactory(clientOwnership));
        policyDir.RegisterDefaultData(new MasterClientDataPolicyFactory(areaState));
        policyDir.RegisterDefaultEvent(new OwnershipEventPolicyFactory(clientOwnership, sideChannel));
        policyDir.RegisterDefaultEvent(new MasterClientEventPolicyFactory(areaState, sideChannel));
        policyDir.RegisterDefaultEvent(new RunOnMasterClientOnlyEventPolicyFactory(clientOwnership, areaState, sideChannel));
        policyDir.RegisterDefaultEvent(new SpawnSummonEventEventPolicyFactory(clientOwnership, playerState, areaState, world, sideChannel));
        policyDir.RegisterDefaultEvent(new AlwaysPropagatesEventPolicyFactory(sideChannel));

        var mappedEvent = MappedEvent = new MappedEventManager(sideChannel, logger);
        var mappingPolicyDir = MappingPolicyDir = new WukongMappingPolicyDirectory(policyDir, mappedEntity, mappedEvent, wukongArchetype);

        var fieldMappingRegistry = new ComponentFieldMappingRegistry();
        FieldMappingRegistry = fieldMappingRegistry;
        RegisterDataMappings(fieldMappingRegistry);

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
        var commandTypeConverter = ArgConverter = new ConsoleArgumentTypeConverter([]);
        var commandMatcher = CommandMatcher = new ConsoleCommandMatcher(commandParser, commandRegistry, commandTypeConverter);
        var commandConsole = CommandConsole = new WukongCommandConsole(commandMatcher, areaState, playerState, eventBus, chatter, widgetManager);
        var wukongInputManager = WukongInputManager = new WukongInputManager(commandConsole, chatter, widgetManager);

        var connectionController = ConnectionController = new WukongLevelTransitionConnectionController(eventBus, connection);

        var pingMonitor = PingMonitor = new NetworkPingMonitor(relayClient);
        var pingWidgetUpdater = PingWidgetUpdater = new PingWidgetUpdater(pingMonitor, serverRpc);

        var runtimeLogger = LoggerFactory.CreateLogger("Runtime");
        var preludeBackend = PreludeBackend = new RuntimeWeaverBackend(runtimeLogger);
        var prelude = Prelude = new RuntimePrelude(preludeBackend, runtimeLogger);

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
        var shimState = new ClientState(
            shimWorld,
            shimNetEntity,
            shimRecorderRelayClient,
            shimEcsLoop,
            jobRegistry,
            areaArchetype,
            playerArchetype,
            shimRecorderLogger
        );

        var shimSynchronizer = new ClientNetworkedStateSynchronizer(
            shimNetEntity,
            shimState,
            jobRegistry,
            netComponentRegistry,
            shimRecorderRelayClient,
            shimEcsLoop,
            clientOwnership,
            shimRecorderLogger
        );

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
        StandardDataMappings = new StandardDataMappings
        {
            PlayerHp = fieldMappingRegistry.Register(MainCharacterComponent.Fields.Hp.In<BUC_AttrContainer>(),
                (ctx, value) =>
                {
                    if (value <= -80000)
                    {
                        Logging.LogError("Would set HP to {HP} but will not (OOB fall damage)", value);
                        return;
                    }
                    
                    if (!value.Equals(ctx.GetFloatValue(EBGUAttrFloat.Hp),
                            Constants.FloatComparisonTolerance))
                    {
                        ctx.SetFloatValue(EBGUAttrFloat.Hp, value);
                    }
                }, (ref main, ctx) =>
                {
                    main.Hp_SetFromGame(ctx.GetFloatValue(EBGUAttrFloat.Hp));
                    if (main.Hp > 0)
                    {
                        main.IsDead_SetFromGame(false);
                    }
                }),

            PlayerHpMax = fieldMappingRegistry.Register(MainCharacterComponent.Fields.HpMaxBase.In<BUC_AttrContainer>(),
                (ctx, value) =>
                {
                    if (!value.Equals(ctx.GetFloatValue(EBGUAttrFloat.HpMaxBase),
                            Constants.FloatComparisonTolerance))
                    {
                        ctx.SetFloatValue(EBGUAttrFloat.HpMaxBase, value);
                    }
                }, ctx => ctx.GetFloatValue(EBGUAttrFloat.HpMaxBase)),

            PlayerAttributes = fieldMappingRegistry.Register(MainCharacterComponent.Fields.Attributes.In<BUC_AttrContainer>(),
                (ctx, attrs) =>
                {
                    foreach (var (attr, value) in attrs)
                    {
                        ctx.SetFloatValue((EBGUAttrFloat)attr, value);
                    }
                }, (ref main, ctx) =>
                {
                    foreach (var attr in Constants.SyncedAttributes)
                    {
                        var value = ctx.GetFloatValue(attr);
                        main.Attributes.SetAttribute((byte)attr, value);
                    }
                }),

            Hp = fieldMappingRegistry.Register(HpComponent.Fields.Hp.In<BUC_AttrContainer>(),
                (ctx, value) =>
                {
                    if (!value.Equals(ctx.GetFloatValue(EBGUAttrFloat.Hp),
                            Constants.FloatComparisonTolerance))
                    {
                        ctx.SetFloatValue(EBGUAttrFloat.Hp, value);
                    }
                },
                ctx => ctx.GetFloatValue(EBGUAttrFloat.Hp)),

            HpMax = fieldMappingRegistry.Register(HpComponent.Fields.HpMaxBase.In<BUC_AttrContainer>(),
                (ctx, value) =>
                {
                    if (!value.Equals(ctx.GetFloatValue(EBGUAttrFloat.HpMaxBase),
                            Constants.FloatComparisonTolerance))
                    {
                        ctx.SetFloatValue(EBGUAttrFloat.HpMaxBase, value);
                    }
                },
                ctx => ctx.GetFloatValue(EBGUAttrFloat.HpMaxBase)),
        };
    }
}