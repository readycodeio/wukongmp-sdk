using ReadyM.Api.DI;
using ReadyM.Api.Multiplayer.Client;

namespace WukongMp.Api;

/// <summary>
/// Keeps a level transition from being read as a lost connection.
/// </summary>
/// <remarks>
/// LiteNetLib's disconnect timeout is wall clock, so a transition that stalls the process for longer
/// than the timeout trips it in one jump, however healthy the socket is. On a machine loading flat out
/// that is around ten seconds against a five second timeout, which is why it reproduces on mid-range
/// hardware and never on a fast desktop. Bracketing the transition tells the client to stop counting
/// it, rather than trying to keep pings flowing through a stall that nothing can schedule around.
/// </remarks>
internal sealed class LevelTransitionStallGuard(IRelayClient relayClient, WukongEventBus eventBus) : IHostedService
{
    public void OnScopeStart()
    {
        eventBus.OnExitLevel += OnExitLevel;
        eventBus.OnLevelLoaded += OnLevelLoaded;
    }

    public void Dispose()
    {
        eventBus.OnExitLevel -= OnExitLevel;
        eventBus.OnLevelLoaded -= OnLevelLoaded;

        // Leaving the window open would keep the timeout stretched for the rest of the session.
        relayClient.EndExpectedStall();
    }

    private void OnExitLevel() => relayClient.BeginExpectedStall();

    private void OnLevelLoaded() => relayClient.EndExpectedStall();
}
