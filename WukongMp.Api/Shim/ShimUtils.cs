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
        // NOTE: LogInformation throughout so these survive a release build's minimum level. The mod loader
        // force-flushes for the whole init window, so the last "step begin" without a matching "step end"
        // names whatever took the process down.
        var logger = container.Resolve<ILogger>();

        var options = new RelayConnectionOptions
        {
            Ticket = ticket,
        };

        if (shimDbPath != null)
        {
            var shimSerializer = new ShimSerializer(container.Resolve<TextRelaySerializer>());
            logger.LogInformation("Loading shim database from: {Path}", shimDbPath);
            var shimDb = shimSerializer.LoadDatabaseMetadata(shimDbPath);

            if (shimDb != null && shimDb.MaxPlayerId != PlayerId.Invalid)
            {
                options.PlayerIdMode = PlayerIdMode.MinId;
                options.PlayerId = new PlayerId((ushort)(shimDb!.MaxPlayerId.RawValue + 1));
            }
        }

        logger.LogInformation("CreateRelayNetworked step begin: Resolve NetworkSessionStats");
        var sessionStats = container.Resolve<NetworkSessionStats>();
        logger.LogInformation("CreateRelayNetworked step end: Resolve NetworkSessionStats");

        logger.LogInformation("CreateRelayNetworked step begin: Resolve ILoggerFactory and create logger");
        var relayLogger = container.Resolve<ILoggerFactory>().CreateLogger("Relay Client");
        logger.LogInformation("CreateRelayNetworked step end: Resolve ILoggerFactory and create logger");

        // NOTE: this is the first thing in the whole startup path that touches LiteNetLib, which is one of the
        // bundled assemblies the native loader replaces with a newer build than the game ships. If a crash
        // lands between this pair, that replacement is the thing to look at.
        logger.LogInformation("CreateRelayNetworked step begin: new RelayClient (first touch of LiteNetLib)");
        var relayClient = new RelayClient(host, port, options, sessionStats, relayLogger, noDisconnect);
        logger.LogInformation("CreateRelayNetworked step end: new RelayClient");

        return relayClient;
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
        var logger = container.Resolve<ILogger>();

        logger.LogInformation("InitRelay step begin: CreateRelayNetworked");
        var relayClient = CreateRelayNetworked(container, host, port, ticket, noDisconnect);
        logger.LogInformation("InitRelay step end: CreateRelayNetworked");

        logger.LogInformation("InitRelay step begin: RelayClient.Attach");
        container.RelayClient.Attach(relayClient);
        logger.LogInformation("InitRelay step end: RelayClient.Attach");
    }
}