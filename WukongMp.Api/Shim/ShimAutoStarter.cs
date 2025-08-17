using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client.Blobs;
using ReadyM.Relay.Client.Host;
using ReadyM.Relay.Client.Shim;
using ReadyM.Relay.Client.State;
using WukongMp.Api.Old;

namespace WukongMp.Api.Shim;

public class ShimAutoStarter : IDisposable
{
    private readonly ClientState _clientState;
    
    private readonly ShimPlaybackRelayClient _playClient;
    
    private readonly ShimRelayRecorder _recorder;
    private readonly WukongEventBus _eventBus;
    private readonly ILogger _recorderLogger;

    private IRelayClient? _recorderRelayClient;
    private IBlobClient? _recorderBlobClient;
    private RelayClientService? _recorderRelayService;
    
    public bool ShouldAutoRecord { get; set; }
    public bool ShouldAutoPlay { get; set; }

    private bool _autoRecordingEnabled;
    private bool _autoPlayingEnabled;
    private Task? _recordingStartedTask;

    public ShimAutoStarter(
        ClientState clientState,
        ShimPlaybackRelayClient playClient,
        ShimRelayRecorder recorder,
        WukongEventBus eventBus,
        ILoggerFactory loggerFactory
    )
    {
        _clientState = clientState;
        
        _playClient = playClient;

        _recorderLogger = loggerFactory.CreateLogger("Recorder Shim");
        _recorder = recorder;
        _eventBus = eventBus;
        
        _eventBus.OnBeginLoadGameplayLevel += OnBeginLoadGameplayLevelHandler;
        _eventBus.OnEndPlayGameplayLevel += OnEndPlayGameplayLevelHandler;

        _recorder.OnAttached += OnAttachedHandler;
        _recorder.OnDetached += OnDetachedHandler;
        _recorder.OnRecordingStarted += OnRecordingStartedHandler;
        _recorder.OnRecordingStopped += OnRecordingStoppedHandler;
    }

    public void Dispose()
    {
        _recordingStartedTask?.GetAwaiter().GetResult();
        
        if (_playClient.IsPlaying)
        {
            _playClient.StopPlaying();
        }
        
        _recorder.OnRecordingStopped -= OnRecordingStoppedHandler;
        _recorder.OnRecordingStarted -= OnRecordingStartedHandler;
        _recorder.OnDetached -= OnDetachedHandler;
        _recorder.OnAttached -= OnAttachedHandler;
        
        _eventBus.OnEndPlayGameplayLevel -= OnEndPlayGameplayLevelHandler;
        _eventBus.OnBeginLoadGameplayLevel -= OnBeginLoadGameplayLevelHandler;
    }

    private void OnAttachedHandler(IRelayClient relayClient)
    {
        _recorderRelayClient = relayClient;
        if (_recorderRelayClient != null)
        {
            _recorderBlobClient = new BlobClient(_recorderRelayClient, _recorderLogger);
            _recorderRelayService = new RelayClientService(_recorderRelayClient, _recorderLogger);
        }
    }

    private void OnDetachedHandler(IRelayClient relayClient)
    {
        _recorderRelayClient = null;
        _recorderBlobClient = null;
        _recorderRelayService!.Dispose();
        _recorderRelayService = null;
    }

    private void OnBeginLoadGameplayLevelHandler()
    {
        if (ShouldAutoPlay)
        {
            _playClient.StartPlaying();
            _autoPlayingEnabled = true;
        }
        if (ShouldAutoRecord)
        {
            _recorder.StartRecording();
            _autoRecordingEnabled = true;
        }
    }

    private void OnEndPlayGameplayLevelHandler()
    {
        if (_autoPlayingEnabled)
        {
            _playClient.StopPlaying();
            _autoPlayingEnabled = false;
        }
        if (_autoRecordingEnabled)
        {
            _recorder.StopRecording();
            _autoRecordingEnabled = false;
        }
    }
    
    private void OnRecordingStartedHandler()
    {
        _recordingStartedTask = Task.Run(OnRecordingStartedAsync);
    }

    private async Task OnRecordingStartedAsync()
    {
        if (_recorderRelayClient == null)
            return;
        if (_recorderRelayService == null)
            return;
        
        _recorderLogger.LogDebug("Connecting to record");
        _recorderRelayService.Start();

        _recorderRelayClient.RequestConnect();
        
        _recorderLogger.LogDebug("Waiting for establishing connection");

        while (true)
        {
            var connected = await _recorderRelayClient.Scheduler.RunFuncAsync(context => context.Connected);
            if (connected)
                break;
        
            await Task.Delay(100);
        }
        
        _recorderLogger.LogDebug("Entering room");

        AreaId? areaId;
        while (true)
        {
            areaId = _clientState.CurrentAreaId;
            if (areaId != null)
                break;
            
            await Task.Delay(100);
        }
        
        _recorderRelayClient.RequestJoinArea(areaId.Value);
        
        _recorderLogger.LogDebug("Requesting saves to record the results for shim");
        var recordSaveRelay = new WukongSaveRelay(_recorderBlobClient!, _recorderLogger);
        var worldSave = await recordSaveRelay.DownloadWorldSaveAsync();
        _recorderLogger.LogDebug("World save downloaded: {WorldSave}, size {Size} bytes", worldSave?.Name, worldSave?.Content.Length);
        var playerSave = await recordSaveRelay.DownloadPlayerSaveAsync();
        _recorderLogger.LogDebug("Player save downloaded: {PlayerSave}, size {Size} bytes", playerSave?.Name, playerSave?.Content.Length);
    }

    private void OnRecordingStoppedHandler()
    {
        if (_recorderRelayClient == null)
            return;
        
        _recorderRelayClient.RequestLeaveArea();
        _recorderRelayClient.Stop();
    }
}