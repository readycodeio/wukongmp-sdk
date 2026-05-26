using System;
using System.Threading.Tasks;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.Host;
using ReadyM.Relay.Client.Shim;
using ReadyM.Relay.Client.State;

namespace WukongMp.Api.Shim;

internal class ShimAutoStarter : IDisposable
{
    private readonly ClientEcsUpdateLoop _ecsLoop;
    private readonly ClientEcsUpdateLoop _shimEcsLoop;

    private readonly ClientState _clientState;
    private readonly WukongEventBus _eventBus;
    private readonly ILogger _logger;

    private readonly ShimPlaybackRelayClient _playbackClient;

    private readonly ShimRelayRecorder _recorder;
    private readonly IRelayClient _recorderRelayClient;
    // private readonly IBlobClient _recorderRelayBlobClient;
    private readonly RelayClientService _recorderRelayService;

    public bool ShouldAutoRecord { get; set; }
    public bool ShouldAutoPlay { get; set; }

    private bool _autoRecordingEnabled;
    private bool _autoPlayingEnabled;
    private Task? _recordingStartedTask;

    public ShimAutoStarter(
        ClientState clientState,
        WukongEventBus eventBus,
        ClientEcsUpdateLoop ecsLoop,
        ClientEcsUpdateLoop shimEcsLoop,
        ShimPlaybackRelayClient playbackClient,
        ShimRelayRecorder recorder,
        // IBlobClient recorderRelayBlobClient,
        RelayClientService recorderRelayService,
        ILogger logger
    )
    {
        _clientState = clientState;
        _eventBus = eventBus;

        _ecsLoop = ecsLoop;
        _shimEcsLoop = shimEcsLoop;

        _playbackClient = playbackClient;

        _recorder = recorder;
        _recorderRelayClient = _recorder.AttachedRelayClient;
        // _recorderRelayBlobClient = recorderRelayBlobClient;
        _recorderRelayService = recorderRelayService;

        _logger = logger;

        _eventBus.OnBeginLoadGameplayLevel += OnBeginLoadGameplayLevelHandler;
        _eventBus.OnEndPlayGameplayLevel += OnEndPlayGameplayLevelHandler;

        _ecsLoop.OnStarted += OnEcsStartedHandler;
        _ecsLoop.OnStopped += OnEcsStoppedHandler;
        // _schedulerSystem.OnUpdateLoop += OnEcsUpdateLoopHandler;

        _recorder.OnRecordingStarted += OnRecordingStartedHandler;
        _recorder.OnRecordingStopped += OnRecordingStoppedHandler;
    }

    public void Dispose()
    {
        _recordingStartedTask?.GetAwaiter().GetResult();

        if (_playbackClient.IsPlaying)
        {
            _playbackClient.StopPlaying();
        }

        _recorder.OnRecordingStopped -= OnRecordingStoppedHandler;
        _recorder.OnRecordingStarted -= OnRecordingStartedHandler;

        // _ecsLoop.OnUpdateLoop -= OnEcsUpdateLoopHandler;
        _ecsLoop.OnStopped -= OnEcsStoppedHandler;
        _ecsLoop.OnStarted -= OnEcsStartedHandler;

        _eventBus.OnEndPlayGameplayLevel -= OnEndPlayGameplayLevelHandler;
        _eventBus.OnBeginLoadGameplayLevel -= OnBeginLoadGameplayLevelHandler;
    }

    private void OnEcsStartedHandler()
    {
        _shimEcsLoop.Start();
    }

    private void OnEcsStoppedHandler()
    {
        _shimEcsLoop.Stop();
    }

    private void OnEcsUpdateLoopHandler(CommandBufferSynced _)
    {
        _shimEcsLoop.Tick(default);
    }

    private void OnBeginLoadGameplayLevelHandler()
    {
        if (ShouldAutoPlay)
        {
            _playbackClient.StartPlaying();
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
            _playbackClient.StopPlaying();
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
        _logger.LogDebug("Connecting to record");
        _recorderRelayService.Start();

        _recorderRelayClient.RequestConnect();

        _logger.LogDebug("Waiting for establishing connection");

        while (true)
        {
            var connected = await _recorderRelayClient.Scheduler.RunFuncAsync(context => context.IsConnected);
            if (connected)
                break;

            await Task.Delay(100);
        }

        _logger.LogDebug("Entering room");

        AreaId? areaId;
        while (true)
        {
            areaId = _clientState.CurrentAreaId;
            if (areaId != null)
                break;

            await Task.Delay(100);
        }

        _recorderRelayClient.RequestJoinArea(areaId.Value);

        // _logger.LogDebug("Requesting saves to record the results for shim");
        // var recordSaveRelay = new CloudWukongSaveApi(_recorderRelayBlobClient, _logger);
        // var worldSave = await recordSaveRelay.DownloadWorldSaveAsync();
        // _logger.LogDebug("World save downloaded: {WorldSave}, size {Size} bytes", worldSave?.Name, worldSave?.Content.Length);
        // var playerSave = await recordSaveRelay.DownloadPlayerSaveAsync();
        // _logger.LogDebug("Player save downloaded: {PlayerSave}, size {Size} bytes", playerSave?.Name, playerSave?.Content.Length);
    }

    private void OnRecordingStoppedHandler()
    {
        _recorderRelayClient.RequestLeaveArea();
        _recorderRelayService.Stop();
    }
}