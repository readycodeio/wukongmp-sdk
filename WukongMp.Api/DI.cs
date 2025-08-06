using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Serialization;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.Blobs;
using ReadyM.Relay.Client.Shim;
using ReadyM.Relay.Client.State;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.ECS.Registry;
using ReadyM.Relay.Common.Serialization;
using ReadyM.Relay.Common.Wukong;
using ReadyM.Relay.Server.Wukong.ECS.Registry;
using WukongMp.Api.Configuration;
using WukongMp.Api.Coop;
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
    public IClientEcsUpdateLoop UpdateLoop { get; private set; } = null!;
    
    public RelaySerializer Serializer { get; private set; } = null!;
    public HotSwappableRelayClient RelayClient { get; private set; } = null!;
    public BlobClient BlobClient { get; set; } = null!;
    public NetworkedEntityManager NetEntity { get; private set; } = null!;

    public ClientState State { get; private set; } = null!;
    public ClientNetworkedEntityState ClientNetEntity { get; private set; } = null!;

    public TextRelaySerializer TextSerializer { get; private set; } = null!;
    public ShimRelayRecorder ShimRecorder { get; private set; } = null!;
    public ShimController ShimController { get; private set; } = null!;
    public ShimRelayClient ShimRelayClient { get; private set; } = null!;
    public ShimAutoStarter ShimAuto { get; set; } = null!;
    
    public AreaComponentRegistry AreaComponentRegistry { get; private set; } = null!;
    public PlayerComponentRegistry PlayerComponentRegistry { get; private set; } = null!;
    public ClientOwnershipManager OwnerManager { get; private set; } = null!;

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
    public INetworkedComponentRegistry NetComponents { get; private set; } = null!;
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
        
        World = new Store(new EntityStore());

        NetEntity = new NetworkedEntityManager(World, Logger, () => RelayClient.PlayerId);

        Serializer = new RelaySerializer([
            new DefaultRelaySerializerRegistration(),
            new WukongSerializerRegistration(),
        ]);
        RelayClient = new HotSwappableRelayClient();
        BlobClient = new BlobClient(RelayClient, Logger);
        
        EventBus = new WukongEventBus();
        
        TextSerializer = new TextRelaySerializer([
            new DefaultTextRelaySerializerRegistration(),
            new WukongTextSerializerRegistration(),
        ]);
        ShimRecorder = new ShimRelayRecorder(LoggerFactory.CreateLogger("Shim Recorder"));
        ShimController = new ShimController(ShimRecorder, TextSerializer, Logger);
        ShimRelayClient = new ShimRelayClient(LoggerFactory.CreateLogger("Play Shim"));
        ShimAuto = new ShimAutoStarter(ShimRelayClient, ShimRecorder, EventBus, LoggerFactory);
        
        UpdateLoop = new ClientEcsUpdateLoop(World, Logger);

        AreaComponentRegistry = new AreaComponentRegistry([
            new WukongAreaRegistration(),
        ]);
        PlayerComponentRegistry = new PlayerComponentRegistry([
            new WukongPlayerRegistration(),
        ]);
        
        pawnState = new WukongPawnState(Players, World, ClientNetEntity);
        ModeManager = new WukongPlayerModeManager(Players, AreaState);
        GameplaySettings = new WukongGameplaySettings(World, AreaState);
        PlayerPawnState = new WukongPlayerPawnState(World, Players, ModeManager);
        
        NetLogger = new WukongNetworkLogger(Logger, World, AreaState, Players, RelayClient);
        NetComponents = new NetworkedComponentRegistry([
            new WukongCoreComponentRegistration(),
        ]);
        Synchronizer = new WukongSynchronizer(World, AreaState, Players, PlayerProperty, ModeManager,
            PlayerPawnState, Rpc, NetEntity, NetComponents, RelayClient, UpdateLoop, SystemRegistry, Logger);
        Connection = new WukongConnectionManager(RelayClient, Players, Synchronizer, AreaState);
        ConnectionController = new WukongLevelTransitionConnectionController(EventBus, Connection, Synchronizer);

        State = new ClientState(World, RelayClient, UpdateLoop, Synchronizer, AreaComponentRegistry, PlayerComponentRegistry);
        AreaState = new WukongAreaState(State);
        PlayerState = new WukongPlayerState(State);
        ClientNetEntity = new ClientNetworkedEntityState(NetEntity, State, Logger);

        OwnerManager = new NetworkedOwnershipManager(World, Logger);

        Rpc = new WukongRpcCallbacks(Serializer, RelayClient, State, AreaState, ClientNetEntity, Players, pawnState);
        SaveRelay = new WukongSaveRelay(BlobClient);

        Chatter = new WukongChatter(Connection, Players, PlayerProperty, Synchronizer, Rpc, GameplaySettings);
        Patcher = new WukongPatcher();
        
        if (Constants.IsCoop)
            Coop = new WukongCoop(Serializer, RelayClient, Players, PlayerProperty, Synchronizer);
        else
            PVP = new WukongPVP(World, Serializer, RelayClient, AreaState, Players, PlayerProperty, EventBus, Synchronizer, Rpc, Chatter);

        Logger.LogDebug("DI Initialized");
    }
}
