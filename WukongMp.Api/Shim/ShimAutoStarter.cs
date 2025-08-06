using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Client.Shim;
using ReadyM.Relay.Common.Protocol;
using WukongMp.Api.Old;

namespace WukongMp.Api.Shim;

public class ShimAutoStarter : IDisposable
{
    private readonly ShimRelayClient _playClient;
    private readonly ShimRelayRecorder _recorder;
    private readonly WukongEventBus _eventBus;
    private readonly ILogger _backgroundLogger;

    public bool ShouldAutoRecord { get; set; }
    public bool ShouldAutoPlay { get; set; }

    private bool _autoRecordingEnabled;
    private bool _autoPlayingEnabled;
    private Task? _backgroundTask;

    public ShimAutoStarter(ShimRelayClient playClient, ShimRelayRecorder recorder, WukongEventBus eventBus, ILoggerFactory loggerFactory)
    {
        _playClient = playClient;
        _recorder = recorder;
        _eventBus = eventBus;
        _backgroundLogger = loggerFactory.CreateLogger("Background Shim");
        
        _eventBus.OnBeginLoadGameplayLevel += OnBeginLoadGameplayLevel;
        _eventBus.OnEndPlayGameplayLevel += OnEndPlayGameplayLevel;
        
        _recorder.OnRecordingStarted += OnRecordingStarted;
        _recorder.OnRecordingStopped += OnRecordingStopped;
    }

    public void Dispose()
    {
        _backgroundTask?.GetAwaiter().GetResult();
        
        if (_playClient.IsPlaying)
        {
            _playClient.StopPlaying();
        }
        
        _recorder.OnRecordingStopped -= OnRecordingStopped;
        _recorder.OnRecordingStarted -= OnRecordingStarted;
        
        _eventBus.OnEndPlayGameplayLevel -= OnEndPlayGameplayLevel;
        _eventBus.OnBeginLoadGameplayLevel -= OnBeginLoadGameplayLevel;
    }

    private void OnBeginLoadGameplayLevel()
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

    private void OnEndPlayGameplayLevel()
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
    
    private void OnRecordingStarted()
    {
        _backgroundTask = Task.Run(OnRecordingStartedAsync);
    }

    private async Task OnRecordingStartedAsync()
    {
        var recordRelayClient = _recorder.RelayClient;
        if (recordRelayClient == null)
            return;
        
        _backgroundLogger.LogDebug("Connecting to record");
        recordRelayClient.Start();

        _backgroundLogger.LogDebug("Waiting for establishing connection");
        while (recordRelayClient.Connected != true)
        {
            await Task.Delay(Constants.ShimClientTickRateMs);
        }
        
        _backgroundLogger.LogDebug("Entering room");
        recordRelayClient.EnterRoom();
        
        _backgroundLogger.LogDebug("Requesting saves to record the results for shim");
        var recordSaveRelay = new WukongSaveRelay(recordRelayClient);
        var worldSave = await recordSaveRelay.DownloadWorldSaveAsync();
        _backgroundLogger.LogDebug("World save downloaded: {WorldSave}, size {Size} bytes", worldSave?.Name, worldSave?.Content.Length);
        var playerSave = await recordSaveRelay.DownloadPlayerSaveAsync();
        _backgroundLogger.LogDebug("Player save downloaded: {PlayerSave}, size {Size} bytes", playerSave?.Name, playerSave?.Content.Length);
    }

    private void OnRecordingStopped()
    {
        var recordRelayClient = _recorder.RelayClient;
        if (recordRelayClient == null)
            return;
        
        recordRelayClient.ExitRoom();
        recordRelayClient.Stop();
    }
}