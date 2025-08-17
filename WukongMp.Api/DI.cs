using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
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
using WukongMp.Api.Configuration;
using WukongMp.Api.Coop;
using WukongMp.Api.ECS.Archetypes;
using WukongMp.Api.ECS.Systems;
using WukongMp.Api.Old;
using WukongMp.Api.PVP;
using WukongMp.Api.Serialization;
using WukongMp.Api.Shim;
using WukongMp.Api.State;

namespace WukongMp.Api;

public class DI
{
    public static DI Instance { get; } = new();

    public ILoggerFactory LoggerFactory { get; private set; } = null!;
    public ILogger Logger { get; private set; } = null!;

    public Store World { get; private set; } = null!;
    public ArchetypeEventRouter archetypeEvent { get; private set; } = null!;
    public IClientEcsUpdateLoop EcsLoop { get; private set; } = null!;
    
    public RelaySerializer Serializer { get; private set; } = null!;
    public HotSwappableRelayClient RelayClient { get; private set; } = null!;
    public BlobClient BlobClient { get; set; } = null!;
    public NetworkedEntityManager NetEntity { get; private set; } = null!;
    public RelayClientService RelayClientService { get; private set; } = null!;

    public ClientState State { get; private set; } = null!;
    public ClientNetworkedEntityState ClientNetEntity { get; private set; } = null!;

    public TextRelaySerializer TextSerializer { get; private set; } = null!;
    public ShimRelayMessageParser ShimParser { get; private set; } = null!;
    public ShimReplayDependencyTracker ShimDepTracker { get; set; } = null!;
    public ShimReplayDependencyTracker shimReplayDependencyTracker { get; private set; } = null!;
    public ShimRelayRecorder ShimRecorder { get; private set; } = null!;
    public ShimController ShimController { get; private set; } = null!;
    public ShimPlaybackRelayClient ShimPlaybackRelayClient { get; private set; } = null!;
    public ShimAutoStarter ShimAuto { get; set; } = null!;
    
    public AreaComponentRegistry AreaComponentRegistry { get; private set; } = null!;
    public PlayerComponentRegistry PlayerComponentRegistry { get; private set; } = null!;
    public NetworkedOwnershipManager OwnershipManager { get; private set; } = null!;
    public ClientOwnershipManager ClientOwnership { get; private set; } = null!;

    public WukongAreaState AreaState { get; private set; } = null!;
    public WukongPlayerState PlayerState { get; private set; } = null!;
    public WukongPawnState PawnState { get; private set; } = null!;
    public WukongPlayerModeManager ModeManager { get; private set; } = null!;
    public WukongGameplaySettings GameplaySettings { get; private set; } = null!;
    public WukongPlayerPawnState PlayerPawnState { get; private set; } = null!;

    public WukongRpcCallbacks Rpc { get; private set; } = null!;
    public WukongSaveRelay SaveRelay { get; private set; } = null!;
    public WukongEventBus EventBus { get; private set; } = null!;
    
    public WukongNetworkLogger NetLogger { get; private set; } = null!;
    public INetworkedComponentRegistry NetComponentRegistry { get; private set; } = null!;
    public JobRegistry JobRegistry { get; private set; } = null!;
    public WukongSynchronizer Synchronizer { get; private set; } = null!;
    public WukongConnectionManager Connection { get; private set; } = null!;
    public WukongLevelTransitionConnectionController ConnectionController { get; private set; } = null!;

    public WukongChatter Chatter { get; private set; } = null!;
    public WukongPatcher Patcher { get; private set; } = null!;
    public WukongPVP? PVP { get; private set; }
    public WukongCoop? Coop { get; private set; }

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
        
        var areaComponentRegistry = AreaComponentRegistry = new AreaComponentRegistry([
            new WukongAreaRegistration(),
        ]);
        var playerComponentRegistry = PlayerComponentRegistry = new PlayerComponentRegistry([
            new WukongPlayerRegistration(),
        ]);
        var areaArchetype = new DefaultAreaArchetypeRegistration(areaComponentRegistry);
        var playerArchetype = new DefaultPlayerArchetypeRegistration(playerComponentRegistry);
        var wukongArchetype = new ClientWukongArchetypeRegistration();
        
        var world = World = new Store(new EntityStore(), [
            areaArchetype,
            playerArchetype,
            wukongArchetype,
        ]);
        
        var worldEvent = archetypeEvent = new ArchetypeEventRouter(world);
        var serializer = Serializer = new RelaySerializer([
            new DefaultRelaySerializerRegistration(),
            new WukongSerializerRegistration(),
        ]);
        
        var relayClient = RelayClient = new HotSwappableRelayClient();
        var blobClient = BlobClient = new BlobClient(relayClient, logger);
        var netEntity = NetEntity = new NetworkedEntityManager(world, logger, relayClient);
        var relayClientService = RelayClientService = new RelayClientService(relayClient, logger);
        
        var eventBus = EventBus = new WukongEventBus();
        
        var textSerializer = TextSerializer = new TextRelaySerializer([
            new DefaultTextRelaySerializerRegistration(),
            new WukongTextSerializerRegistration(),
            new ClientShimTextSerializerRegistration(),
        ]);

        var shimParser = ShimParser = new ShimRelayMessageParser([
            new BlobClientShimParserImpl(),
            new ClientSynchronizerShimParserImpl(netEntity, logger),
        ]);
        var shimDepTracker = ShimDepTracker = new ShimReplayDependencyTracker([
            new BlobClientShimTrackerImpl(),
            new ClientSynchronizerShimTrackerImpl(),
        ]);
        var shimRecorder = ShimRecorder = new ShimRelayRecorder(shimParser, loggerFactory.CreateLogger("Shim Recorder"));
        var shimController = ShimController = new ShimController(shimRecorder, textSerializer, logger);
        var shimRelayClient = ShimPlaybackRelayClient = new ShimPlaybackRelayClient(
            shimDepTracker,
            shimParser,
            loggerFactory.CreateLogger("Play Shim")
        );
        
        var ecsLoop = EcsLoop = new ClientEcsUpdateLoop(world, logger);

        var netComponentRegistry = NetComponentRegistry = new NetworkedComponentRegistry([
            new DefaultNetworkedComponentRegistration(),
            new WukongNetworkedComponentRegistration(),
        ]);
        var jobRegistry = JobRegistry = new JobRegistry(netComponentRegistry, netEntity, relayClient, logger);
 
        var state = State = new ClientState(world, netEntity, relayClient, ecsLoop, jobRegistry, areaArchetype, playerArchetype, logger);
        var areaState = AreaState = new WukongAreaState(state);
        var clientNetEntity = ClientNetEntity = new ClientNetworkedEntityState(netEntity, state, logger);
        var playerState = PlayerState = new WukongPlayerState(world, wukongArchetype, clientNetEntity, state, logger);
        
        var shimAuto = ShimAuto = new ShimAutoStarter(state, shimRelayClient, shimRecorder, eventBus, loggerFactory);

        var pawnState = PawnState = new WukongPawnState(world, wukongArchetype, clientNetEntity, logger);
        var modeManager = ModeManager = new WukongPlayerModeManager(state, areaState);
        var gameplaySettings = GameplaySettings = new WukongGameplaySettings(world, areaState);
        var playerPawnState = PlayerPawnState = new WukongPlayerPawnState(world, state, playerState, modeManager, logger);
        
        var connection = Connection = new WukongConnectionManager(relayClientService, state, playerState, areaState, logger);
        var netLogger = NetLogger = new WukongNetworkLogger(world, state, areaState, playerState, logger);
        var synchronizer = Synchronizer = new WukongSynchronizer(
            worldEvent,
            state,
            wukongArchetype,
            areaState,
            playerState,
            playerPawnState,
            modeManager,
            netEntity,
            jobRegistry, 
            netComponentRegistry,
            relayClient,
            ecsLoop,
            logger);
        var connectionController = ConnectionController = new WukongLevelTransitionConnectionController(eventBus, connection, synchronizer);

        var ownershipManager = OwnershipManager = new NetworkedOwnershipManager(world, logger);
        var clientOwnership = ClientOwnership = new ClientOwnershipManager(state, ownershipManager);

        var rpc = Rpc = new WukongRpcCallbacks(serializer, relayClient, state, areaState, clientNetEntity, playerState, pawnState, ecsLoop, logger);
        var saveRelay = SaveRelay = new WukongSaveRelay(blobClient, logger);

        var chatter = Chatter = new WukongChatter(connection, state, playerState, rpc, gameplaySettings);
        var patcher = Patcher = new WukongPatcher();

        if (Constants.IsCoop)
            Coop = new WukongCoop(serializer, relayClient, areaState, playerState, synchronizer);
        else
            PVP = new WukongPVP(world, serializer, relayClient, state, areaState, playerState, eventBus, synchronizer, rpc, chatter, ecsLoop, logger);

        Logger.LogDebug("DI Initialized");
    }
}
