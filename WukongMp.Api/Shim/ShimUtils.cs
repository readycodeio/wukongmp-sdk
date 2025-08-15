using System;
using System.IO;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.Shim;

namespace WukongMp.Api.Shim;

public static class ShimUtils
{
    private static RelayClient CreateRelayNetworked(DI container, string host, int port, Guid userGuid, bool noDisconnect, string? shimDbPath = null)
    {
        var options = new RelayConnectionOptions()
        {
            UserGuid = userGuid,
        };

        if (shimDbPath != null)
        {
            var shimSerializer = new ShimSerializer(container.TextSerializer);
            container.Logger.LogInformation("Loading shim database from: {Path}", shimDbPath);
            var shimDb = shimSerializer.LoadDatabaseMetadata(shimDbPath);

            if (shimDb != null && shimDb.MaxPlayerId != PlayerId.Invalid)
            {
                options.PlayerIdMode = PlayerIdMode.MinId;
                options.PlayerId = new PlayerId((ushort)(shimDb!.MaxPlayerId.RawValue + 1));
            }
        }
        
        var relayClient = new RelayClient(host, port, options, noDisconnect, container.LoggerFactory.CreateLogger("Relay Client"));
        return relayClient;
    }

    public static void InitRelayPlayShim(DI container, string shimPath)
    {
        var shimSerializer = new ShimSerializer(container.TextSerializer);

        container.Logger.LogInformation("Loading shim recording from: {Path}", shimPath);
        var recording = shimSerializer.Load(shimPath);
        container.ShimRelayClient.SetRecording(recording!);

        container.RelayClient.Attach(container.ShimRelayClient);

        container.ShimAuto.ShouldAutoPlay = true;
    }

    public static void InitRelayRecordShim(DI container, string host, int port, Guid userGuid, bool noDisconnect, string shimPath)
    {
        var shimDbPath = Path.GetDirectoryName(shimPath);
        
        var relayClient = CreateRelayNetworked(container, host, port, userGuid, noDisconnect, shimDbPath);

        AttachRecording(container, host, port, noDisconnect);
        
        container.RelayClient.Attach(relayClient);
        
        container.ShimAuto.ShouldAutoRecord = true;
    }
    
    private static void AttachRecording(DI container, string host, int port, bool noDisconnect)
    {
        var recordGuid = new Guid("deadbeef-3333-3333-3333-deadbeef0001");
        var recordOptions = new RelayConnectionOptions()
        {
            UserGuid = recordGuid,
            PlayerIdMode = PlayerIdMode.ExactId,
            PlayerId = new PlayerId(255),
        };
        var recordRelayClient = new RelayClient(
            host,
            port,
            recordOptions,
            noDisconnect,
            container.LoggerFactory.CreateLogger("Recorder Relay")
        );

        var recording = new ShimRecording();
        
        container.ShimRecorder.SetRecording(recording);
        container.ShimRecorder.Attach(recordRelayClient);
    }

    public static void InitRelay(DI container, string host, int port, Guid userGuid, bool noDisconnect)
    {
        var relayClient = CreateRelayNetworked(container, host, port, userGuid, noDisconnect);
        
        container.RelayClient.Attach(relayClient);
    }
}