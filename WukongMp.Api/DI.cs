using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.Shim;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.Serialization;
using ReadyM.Relay.Common.Wukong;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old;
using WukongMp.Api.Old.State;

namespace WukongMp.Api;

public class DI
{
    public static DI Instance { get; } = new();

    public ILoggerFactory LoggerFactory { get; private set; } = null!;
    public ILogger Logger { get; private set; } = null!;

    public Store World { get; private set; } = null!;
    public WukongUpdateLoop UpdateLoop { get; private set; } = null!;
    public ISystemRegistry SystemRegistry { get; private set; } = null!;
    public EntityManagerWithLogs EntityManager { get; private set; } = null!;
    
    public RelaySerializer Serializer { get; private set; } = null!;
    public HotSwappableRelayClient RelayClient { get; private set; } = null!;
    public NetworkedEntityManager NetManager { get; private set; } = null!;

    public TextRelaySerializer TextSerializer { get; private set; } = null!;
    public ShimRelayRecorder ShimRecorder { get; private set; } = null!;
    public ShimController ShimController { get; private set; } = null!;
    public ShimRelayClient ShimRelayClient { get; private set; } = null!;
    public ShimAutoStarter ShimAuto { get; set; } = null!;
    
    public RoomStateProxy RoomState { get; private set; } = null!;
    public WukongPlayerRegistry Players { get; private set; } = null!;
    public WukongPlayerPropertyManager PlayerProperty { get; private set; } = null!;

    public WukongPawnRegistry PawnRegistry { get; private set; } = null!;
    public WukongPlayerModeManager ModeManager { get; private set; } = null!;
    public WukongGameplaySettings GameplaySettings { get; private set; } = null!;
    public WukongPlayerPawnManager PlayerPawnManager { get; private set; } = null!;

    public WukongRpcCallbacks Rpc { get; private set; } = null!;
    public WukongServerRpcCallbacks ServerRpc { get; private set; } = null!;
    public WukongSaveRelay SaveRelay { get; private set; } = null!;
    public WukongEventBus EventBus { get; private set; } = null!;
    
    public WukongNetworkLogger NetLogger { get; private set; } = null!;
    public INetworkedComponentRegistry NetComponents { get; private set; } = null!;
    public WukongSynchronizer Synchronizer { get; private set; } = null!;
    public WukongConnectionManager Connection { get; private set; } = null!;
    public WukongLevelTransitionConnectionController ConnectionController { get; private set; } = null!;
    public NetworkPingMonitor PingMonitor { get; private set; } = null!;
    public PingWidgetUpdater PingWidgetUpdater { get; private set; } = null!;

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
        SystemRegistry = new SystemRegistry(World);

        NetManager = new NetworkedEntityManager(World, () => RelayClient.PlayerId);
        EntityManager = new EntityManagerWithLogs(NetManager);

        Serializer = new RelaySerializer([
            new DefaultRelaySerializerRegistration(),
            new WukongSerializerRegistration(),
        ]);
        RelayClient = new HotSwappableRelayClient();
        
        EventBus = new WukongEventBus();
        
        TextSerializer = new TextRelaySerializer([
            new DefaultTextRelaySerializerRegistration(),
            new WukongTextSerializerRegistration(),
        ]);
        ShimRecorder = new ShimRelayRecorder(LoggerFactory.CreateLogger("Shim Recorder"));
        ShimController = new ShimController(ShimRecorder, TextSerializer, Logger);
        ShimRelayClient = new ShimRelayClient(LoggerFactory.CreateLogger("Play Shim"));
        ShimAuto = new ShimAutoStarter(ShimRelayClient, ShimRecorder, EventBus, LoggerFactory);
        
        RoomState = new RoomStateProxy(RelayClient);
        Players = new WukongPlayerRegistry();
        PlayerProperty = new WukongPlayerPropertyManager(RelayClient, Players);
        UpdateLoop = new WukongUpdateLoop(World, PlayerProperty);

        PawnRegistry = new WukongPawnRegistry(Players, World, EntityManager, SystemRegistry);
        ModeManager = new WukongPlayerModeManager(Players, RoomState);
        GameplaySettings = new WukongGameplaySettings(World, RelayClient);
        PlayerPawnManager = new WukongPlayerPawnManager(World, Players, ModeManager);
        
        Rpc = new WukongRpcCallbacks(Serializer, RelayClient, EntityManager, Players, PawnRegistry);
        ServerRpc = new WukongServerRpcCallbacks(Serializer, RelayClient);
        SaveRelay = new WukongSaveRelay(RelayClient);

        NetLogger = new WukongNetworkLogger(Logger, World, RoomState, Players, RelayClient);
        NetComponents = new NetworkedComponentRegistry([
            new WukongCoreComponentRegistration(),
        ]);
        Synchronizer = new WukongSynchronizer(World, RoomState, Players, PlayerProperty, ModeManager,
            PlayerPawnManager, Rpc, NetManager, NetComponents, RelayClient, UpdateLoop, SystemRegistry, Logger);
        Connection = new WukongConnectionManager(RelayClient, Players, Synchronizer, RoomState);
        ConnectionController = new WukongLevelTransitionConnectionController(EventBus, Connection, Synchronizer);
        
        PingMonitor = new NetworkPingMonitor(RelayClient);
        PingWidgetUpdater = new PingWidgetUpdater(PingMonitor);

        Chatter = new WukongChatter(Connection, Players, PlayerProperty, Synchronizer, Rpc, GameplaySettings);
        Patcher = new WukongPatcher();
        
        if (Constants.IsCoop)
            Coop = new WukongCoop(Serializer, RelayClient, Players, PlayerProperty, Synchronizer);
        else
            PVP = new WukongPVP(World, Serializer, RelayClient, RoomState, Players, PlayerProperty, EventBus, Synchronizer, Rpc, Chatter);

        Logger.LogDebug("DI Initialized");
    }
}
