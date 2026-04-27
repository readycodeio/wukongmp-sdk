using System.IO;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Serialization;
using ReadyM.Api.Multiplayer.Shim;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.Utilities;

namespace WukongMp.Api.Shim;

internal static class ShimUtils
{
    private static RelayClient CreateRelayNetworked(IDependencyContainer container, string host, int port, ConnectionTicket ticket, bool noDisconnect, string? shimDbPath = null)
    {
        var options = new RelayConnectionOptions
        {
            Ticket = ticket,
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
        // TODO: Refactor
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

    internal static void InitRelayRecordShim(IDependencyContainer container, string host, int port, ConnectionTicket ticket, bool noDisconnect, string shimPath)
    {
        var shimDbPath = Path.GetDirectoryName(shimPath);

        var relayClient = CreateRelayNetworked(container, host, port, ticket, noDisconnect, shimDbPath);

        AttachRecording(container, host, port, noDisconnect);

        container.Resolve<HotSwappableRelayClient>().Attach(relayClient);
        container.Resolve<ShimAutoStarter>().ShouldAutoRecord = true;
    }

    private static void AttachRecording(IDependencyContainer container, string host, int port, bool noDisconnect)
    {
        var recordTicket = ConnectionTicket.Parse("deadbeef-3333-3333-3333-deadbeef0001");
        var recordOptions = new RelayConnectionOptions
        {
            Ticket = recordTicket,
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

    internal static void InitRelay(DI container, string host, int port, ConnectionTicket ticket, bool noDisconnect)
    {
        var relayClient = CreateRelayNetworked(container, host, port, ticket, noDisconnect);

        container.RelayClient.Attach(relayClient);
    }
}