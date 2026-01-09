using CSharpModBase;
using CSharpModBase.Input;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using PreludeLib.Runtime.Public;
using PreludeLib.Runtime.Backend.WeaverCallback;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.Blobs;
using ReadyM.Relay.Client.Host;
using ReadyM.Relay.Client.Serialization;
using ReadyM.Relay.Client.Shim;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common.ECS.Archetypes;
using ReadyM.Relay.Common.ECS.Jobs;
using ReadyM.Relay.Common.ECS.Registry;
using ReadyM.Relay.Common.Serialization;
using ReadyM.Relay.Common.Wukong.ECS.Registry;
using WukongMp.Api.Chat;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Managers;
using WukongMp.Api.Https;
using WukongMp.Api.Serialization;
using WukongMp.Api.Shim;
using WukongMp.Api.State;
using WukongMp.Api.Tests;
using WukongMp.Api.UI;
using WukongMp.Api.Command;
using WukongMp.Api.Input;

namespace WukongMp.Api;

public sealed class DI
{
    public static DI Instance { get; } = new();

    public IInputManager InputManager { get; private set; } = null!;
    public ILoggerFactory LoggerFactory { get; private set; } = null!;
    public ILogger Logger { get; private set; } = null!;

    public Store World { get; private set; } = null!;
    public ClientWukongArchetypeRegistration ArchetypeRegistration { get; private set; } = null!;
    public ArchetypeEventRouter ArchetypeEvent { get; private set; } = null!;
    public IClientEcsUpdateLoop EcsLoop { get; private set; } = null!;

    public RelaySerializer Serializer { get; private set; } = null!;
    public HotSwappableRelayClient RelayClient { get; private set; } = null!;
    public IBlobClient BlobClient { get; set; } = null!;
    public NetworkedEntityManager NetEntity { get; private set; } = null!;
    public RelayClientService RelayClientService { get; private set; } = null!;

    public ClientState State { get; private set; } = null!;
    public ClientNetworkedEntityState ClientNetEntity { get; private set; } = null!;

    public TextRelaySerializer TextSerializer { get; private set; } = null!;

    public AreaComponentRegistry AreaComponentRegistry { get; private set; } = null!;
    public PlayerComponentRegistry PlayerComponentRegistry { get; private set; } = null!;
    public NetworkedOwnershipManager OwnershipManager { get; private set; } = null!;
    public ClientOwnershipManager ClientOwnership { get; private set; } = null!;

    public WukongAreaState AreaState { get; private set; } = null!;
    public WukongPlayerState PlayerState { get; private set; } = null!;
    public WukongPawnState PawnState { get; private set; } = null!;
    public WukongPlayerModeManager ModeManager { get; private set; } = null!;
    public WukongPlayerPawnState PlayerPawnState { get; private set; } = null!;

    public WukongRpcCallbacks Rpc { get; private set; } = null!;
    public WukongServerRpcCallbacks ServerRpc { get; private set; } = null!;
    public WukongSaveRelay SaveRelay { get; private set; } = null!;
    public WukongEventBus EventBus { get; private set; } = null!;
    public GameplayConfiguration GameplayConfiguration { get; private set; } = null!;
    public GameplayEventRouter GameplayEventRouter { get; private set; } = null!;
    public ColliderDisableData ColliderDisableData { get; private set; } = null!;

    public WukongNetworkLogger NetLogger { get; private set; } = null!;
    public INetworkedComponentRegistry NetComponentRegistry { get; private set; } = null!;
    public JobRegistry JobRegistry { get; private set; } = null!;
    public WukongSynchronizer Synchronizer { get; private set; } = null!;
    public WukongConnectionManager Connection { get; private set; } = null!;
    public WukongLevelTransitionConnectionController ConnectionController { get; private set; } = null!;
    public NetworkPingMonitor PingMonitor { get; private set; } = null!;
    public PingWidgetUpdater PingWidgetUpdater { get; private set; } = null!;
    public FreeCameraManager FreeCameraManager { get; private set; } = null!;
    public GameStateSynchronizer GameStateSynchronizer { get; private set; } = null!;

    public WukongCommandConsole CommandConsole { get; set; } = null!;
    public WukongChatter Chatter { get; private set; } = null!;
    public WukongInputManager WukongInputManager { get; private set; } = null!;

    public RuntimePrelude Prelude { get; private set; } = null!;
    public RuntimeWeaverBackend PreludeBackend { get; private set; } = null!;


    public WukongWidgetManager WidgetManager { get; private set; } = null!;

    public ShimRelayMessageParser ShimParser { get; private set; } = null!;
    public ShimReplayDependencyTracker ShimDepTracker { get; set; } = null!;
    public ShimReplayDependencyTracker ShimReplayDependencyTracker { get; private set; } = null!;
    public HotSwappableRelayClient ShimRecorderRelayClient { get; set; } = null!;
    public ShimRelayRecorder ShimRecorder { get; private set; } = null!;
    public ShimController ShimController { get; private set; } = null!;
    public ShimPlaybackRelayClient ShimPlaybackRelayClient { get; private set; } = null!;
    public ClientEcsUpdateLoop ShimEcsLoop { get; set; } = null!;
    public RelayClientService ShimRelayClientService { get; set; } = null!;
    public NetworkedEntityManager ShimNetEntity { get; set; } = null!;
    public BlobClient ShimRelayBlobClient { get; set; } = null!;

    public ShimAutoStarter ShimAuto { get; set; } = null!;

    public TestsRunner TestsRunner { get; set; } = null!;

    public void InitLogging(ILoggerFactory loggerFactory)
    {
        LoggerFactory = loggerFactory;
        Logger = LoggerFactory.CreateLogger("");
    }

    public void Init()
    {
        Logger.LogDebug("Initializing DI...");

        var loggerFactory = LoggerFactory;
        var logger = Logger;

        var inputManager = InputManager = CSharpModBase.InputManager.Instance;

        var areaComponentRegistry = AreaComponentRegistry = new AreaComponentRegistry([
            new WukongAreaRegistration(),
        ]);
        var playerComponentRegistry = PlayerComponentRegistry = new PlayerComponentRegistry([
            new WukongPlayerRegistration(),
        ]);
        var areaArchetype = new DefaultAreaArchetypeRegistration(areaComponentRegistry);
        var playerArchetype = new DefaultPlayerArchetypeRegistration(playerComponentRegistry);
        var wukongArchetype = ArchetypeRegistration = new ClientWukongArchetypeRegistration();

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
        var netEntity = NetEntity = new NetworkedEntityManager(world, logger, relayClient);
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

        var netComponentRegistry = NetComponentRegistry = new NetworkedComponentRegistry([
            new DefaultNetworkedComponentRegistration(),
            new WukongNetworkedComponentRegistration(),
        ]);
        var jobRegistry = JobRegistry = new JobRegistry(netComponentRegistry, netEntity, relayClient, logger);

        var state = State = new ClientState(world, netEntity, relayClient, ecsLoop, jobRegistry, areaArchetype, playerArchetype, logger);
        var clientNetEntity = ClientNetEntity = new ClientNetworkedEntityState(netEntity, state, logger);
        var playerState = PlayerState = new WukongPlayerState(world, wukongArchetype, clientNetEntity, state, logger);

        var widgetManager = WidgetManager = new WukongWidgetManager(state, playerState);

        var pawnState = PawnState = new WukongPawnState(world, wukongArchetype, clientNetEntity);
        var playerPawnState = PlayerPawnState = new WukongPlayerPawnState(world, playerState, logger);

        var ownershipManager = OwnershipManager = new NetworkedOwnershipManager(world, logger);
        var clientOwnership = ClientOwnership = new ClientOwnershipManager(state, ownershipManager);

        var freeCameraManager = FreeCameraManager = new FreeCameraManager();

        var gameStateSynchronizer = GameStateSynchronizer = new GameStateSynchronizer(state, playerState);

        var colliderDisableData = ColliderDisableData = new ColliderDisableData(playerState, logger);
        var areaState = AreaState = new WukongAreaState(state, world, clientOwnership);
        var modeManager = ModeManager = new WukongPlayerModeManager(state, gameplayEventRouter, freeCameraManager);

        var connection = Connection = new WukongConnectionManager(relayClientService, state, playerState, areaState, logger);
        var netLogger = NetLogger = new WukongNetworkLogger(world, state, areaState, playerState, logger);

        var rpc = Rpc = new WukongRpcCallbacks(serializer, relayClient, state, areaState, clientNetEntity, playerState, pawnState, clientOwnership, freeCameraManager, gameplayEventRouter, ecsLoop, logger);
        var serverRpc = ServerRpc = new WukongServerRpcCallbacks(relayClient, ecsLoop, logger, widgetManager);
        var saveRelay = SaveRelay = new WukongSaveRelay(blobClient, logger);

        var chatter = Chatter = new WukongChatter(state, playerState, rpc, widgetManager);
        var commandConsole = CommandConsole = new WukongCommandConsole(connection, playerState, rpc, Chatter, widgetManager, ecsLoop);
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
        var shimBlobClient = ShimRelayBlobClient = new BlobClient(shimRecorderRelayClient, shimRecorderLogger);
        var shimNetEntity = ShimNetEntity = new NetworkedEntityManager(shimWorld, shimRecorderLogger, shimRecorderRelayClient);

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
}