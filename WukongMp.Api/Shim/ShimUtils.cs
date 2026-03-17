using System;
using System.IO;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Serialization;
using ReadyM.Api.Multiplayer.Shim;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.Utilities;

namespace WukongMp.Api.Shim;

internal static class ShimUtils
{
    private static RelayClient CreateRelayNetworked(IDependencyContainer container, string host, int port, Guid userGuid, bool noDisconnect, string? shimDbPath = null)
    {
        var options = new RelayConnectionOptions()
        {
            UserGuid = userGuid,
        };

        if (shimDbPath != null)
        {
            var shimSerializer = new ShimSerializer(container.Resolve<TextRelaySerializer>());
            container.Resolve<ILogger>().LogInformation("Loading shim database from: {Path}", shimDbPath);
            var shimDb = shimSerializer.LoadDatabaseMetadata(shimDbPath);

            if (shimDb != null && shimDb.MaxPlayerId != PlayerId.Invalid)
            {
                options.PlayerIdMode = PlayerIdMode.MinId;
                options.PlayerId = new PlayerId((ushort)(shimDb!.MaxPlayerId.RawValue + 1));
            }
        }

        return new RelayClient(host, port, options, container.Resolve<NetworkSessionStats>(), container.Resolve<ILoggerFactory>().CreateLogger("Relay Client"), noDisconnect);
    }

    internal static void InitRelayPlayShim(IDependencyContainer container, string shimPath)
    {
        // var shimSerializer = new ShimSerializer(container.TextSerializer);
        //
        // container.Logger.LogInformation("Loading shim recording from: {Path}", shimPath);
        // var recording = shimSerializer.Load(shimPath);
        // container.ShimPlaybackRelayClient.SetRecording(recording!);
        //
        // container.RelayClient.Attach(container.ShimPlaybackRelayClient);
        //
        // container.ShimAuto.ShouldAutoPlay = true;
    }

    internal static void InitRelayRecordShim(IDependencyContainer container, string host, int port, Guid userGuid, bool noDisconnect, string shimPath)
    {
        var shimDbPath = Path.GetDirectoryName(shimPath);
        
        var relayClient = CreateRelayNetworked(container, host, port, userGuid, noDisconnect, shimDbPath);

        AttachRecording(container, host, port, noDisconnect);
        
        container.Resolve<HotSwappableRelayClient>().Attach(relayClient);
        container.Resolve<ShimAutoStarter>().ShouldAutoRecord = true;
    }
    
    private static void AttachRecording(IDependencyContainer container, string host, int port, bool noDisconnect)
    {
        var recordGuid = new Guid("deadbeef-3333-3333-3333-deadbeef0001");
        var recordOptions = new RelayConnectionOptions
        {
            UserGuid = recordGuid,
            PlayerIdMode = PlayerIdMode.ExactId,
            PlayerId = new PlayerId(255),
        };
        var recordRelayClient = new RelayClient(
            host,
            port,
            recordOptions,
            container.Resolve<NetworkSessionStats>(),
            container.Resolve<ILoggerFactory>().CreateLogger("Recorder Relay"),
            noDisconnect
        );

        container.Resolve<HotSwappableRelayClient>().Attach(recordRelayClient);
    }

    internal static void InitRelay(DI container, string host, int port, Guid userGuid, bool noDisconnect)
    {
        var relayClient = CreateRelayNetworked(container, host, port, userGuid, noDisconnect);
        
        container.RelayClient.Attach(relayClient);
    }
}