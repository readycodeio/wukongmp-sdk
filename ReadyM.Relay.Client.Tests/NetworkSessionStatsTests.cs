using LiteNetLib;
using ReadyM.Relay.Client.Utilities;

namespace ReadyM.Relay.Client.Tests;

public sealed class NetworkSessionStatsTests
{
    private NetworkSessionStats MakeStats() => new NetworkSessionStats("testSession", 0, maxValidPingMs: 1000);
    
    // ---------------------------
    // Ping tests
    // ---------------------------

    [Fact]
    public void Defaults_WhenNoData_ReturnZeroes()
    {
        var s = MakeStats();

        Assert.Equal(0, s.TotalPingPackets);
        Assert.Equal(0, s.LostPingPackets);
        Assert.Equal(0.0, s.PingLossRate);
        Assert.Equal(0, s.CurrentLossStreak);
        Assert.Equal(0, s.MaxLossStreak);

        Assert.Equal(0.0, s.PingMeanMs);
        Assert.Equal(0, s.PingMedianMs);
        Assert.Equal(0, s.PingP90Ms);
        Assert.Equal(0, s.PingP95Ms);
        Assert.Equal(0, s.PingP98Ms);

        Assert.Equal(0.0, s.UploadMeanBps);
        Assert.Equal(0.0, s.UploadMedianBps);
        Assert.Equal(0.0, s.UploadP90Bps);
        Assert.Equal(0.0, s.UploadP95Bps);
        Assert.Equal(0.0, s.UploadP98Bps);

        Assert.Equal(0.0, s.DownloadMeanBps);
        Assert.Equal(0.0, s.DownloadMedianBps);
        Assert.Equal(0.0, s.DownloadP90Bps);
        Assert.Equal(0.0, s.DownloadP95Bps);
        Assert.Equal(0.0, s.DownloadP98Bps);

        Assert.Equal(0, s.TransferSamples);
    }

    [Fact]
    public void Ping_AddingValidSamples_UpdatesMeanAndPercentilesExactly()
    {
        // Values: 10,20,30,40,50 (N=5)
        // nearest-rank:
        // p50 rank=ceil(0.5*5)=3 => 30
        // p90 rank=ceil(0.9*5)=5 => 50
        var s = MakeStats();

        s.AddPing(10);
        s.AddPing(20);
        s.AddPing(30);
        s.AddPing(40);
        s.AddPing(50);

        Assert.Equal(5, s.TotalPingPackets);
        Assert.Equal(0, s.LostPingPackets);
        Assert.Equal(0.0, s.PingLossRate);

        Assert.Equal(30.0, s.PingMeanMs, 6);
        Assert.Equal(30, s.PingMedianMs);
        Assert.Equal(50, s.PingP90Ms);
        Assert.Equal(50, s.PingP95Ms);
        Assert.Equal(50, s.PingP98Ms);
    }

    [Fact]
    public void Ping_AboveMax_IsCountedAsLoss_AndDoesNotAffectLatencyDistribution()
    {
        var s = MakeStats();

        s.AddPing(100);
        s.AddPing(2000); // loss
        s.AddPing(300);

        Assert.Equal(3, s.TotalPingPackets);
        Assert.Equal(1, s.LostPingPackets);
        Assert.Equal(1.0 / 3.0, s.PingLossRate, 10);

        // Only valid samples are [100,300]
        Assert.Equal(200.0, s.PingMeanMs, 6);
        Assert.Equal(100, s.PingMedianMs); // N=2, rank=ceil(0.5*2)=1 => 100 (nearest-rank)
        Assert.Equal(300, s.PingP90Ms); // rank=ceil(0.9*2)=2 => 300
    }

    [Fact]
    public void Ping_LossStreak_IsTracked_Correctly()
    {
        var s = new NetworkSessionStats("dummy", 0, maxValidPingMs: 1000);

        // Two losses in a row
        s.AddPing(2001);
        s.AddPing(2002);

        Assert.Equal(2, s.TotalPingPackets);
        Assert.Equal(2, s.LostPingPackets);
        Assert.Equal(2, s.CurrentLossStreak);
        Assert.Equal(2, s.MaxLossStreak);

        // Valid sample resets current streak
        s.AddPing(50);
        Assert.Equal(0, s.CurrentLossStreak);
        Assert.Equal(2, s.MaxLossStreak);

        // Another loss streak of 3
        s.AddPing(2003);
        s.AddPing(2004);
        s.AddPing(2005);

        Assert.Equal(3, s.CurrentLossStreak);
        Assert.Equal(3, s.MaxLossStreak);
    }

    // ---------------------------
    // Transfer tests
    // ---------------------------

    [Fact]
    public void Transfer_FirstUpdate_SetsBaseline_NoSamplesAdded()
    {
        var s = MakeStats();
        var ns = new NetStatistics();

        // Baseline at t=0
        s.UpdateTransfer(ns, nowSeconds: 0.0);

        Assert.Equal(0, s.TransferSamples);
        Assert.Equal(0.0, s.UploadMeanBps);
        Assert.Equal(0.0, s.DownloadMeanBps);
    }

    [Fact]
    public void Transfer_ComputesBytesPerSecond_FromDeltas_AndTracksPercentiles()
    {
        var s = MakeStats();
        var ns = new NetStatistics();

        // Baseline (0 bytes at t=0)
        s.UpdateTransfer(ns, nowSeconds: 0.0);

        // Add 1000 sent / 2000 recv over 1 second
        ns.AddBytesSent(1000);
        ns.AddBytesReceived(2000);
        s.UpdateTransfer(ns, nowSeconds: 1.0);

        // Add 3000 sent / 1000 recv over next 1 second
        ns.AddBytesSent(3000);
        ns.AddBytesReceived(1000);
        s.UpdateTransfer(ns, nowSeconds: 2.0);

        // Upload samples: [1000, 3000]
        // Download samples: [2000, 1000]
        Assert.Equal(2, s.TransferSamples);

        Assert.Equal(2000.0, s.UploadMeanBps, 6);
        Assert.Equal(1000.0, s.UploadMedianBps, 6); // nearest-rank p50 of 2 samples -> rank=1 -> min
        Assert.Equal(3000.0, s.UploadP90Bps, 6);

        Assert.Equal(1500.0, s.DownloadMeanBps, 6);
        Assert.Equal(1000.0, s.DownloadMedianBps, 6);
        Assert.Equal(2000.0, s.DownloadP90Bps, 6);
    }

    [Fact]
    public void Transfer_NonUnitDt_NormalizesByDeltaTime()
    {
        var s = MakeStats();
        var ns = new NetStatistics();

        s.UpdateTransfer(ns, nowSeconds: 0.0);

        // 1000 bytes in 0.5s => 2000 Bps
        ns.AddBytesSent(1000);
        s.UpdateTransfer(ns, nowSeconds: 0.5);

        Assert.Equal(1, s.TransferSamples);
        Assert.Equal(2000.0, s.UploadMeanBps, 6);
    }

    [Fact]
    public void Transfer_IgnoresNonPositiveDt_DoesNotAddSamples()
    {
        var s = MakeStats();
        var ns = new NetStatistics();

        s.UpdateTransfer(ns, nowSeconds: 1.0);

        ns.AddBytesSent(1000);
        s.UpdateTransfer(ns, nowSeconds: 1.0); // dt = 0 => ignored

        Assert.Equal(0, s.TransferSamples);
    }

    // ---------------------------
    // Reset tests
    // ---------------------------

    [Fact]
    public void Reset_ClearsAllStats()
    {
        var s = MakeStats();
        var ns = new NetStatistics();

        s.AddPing(10);
        s.AddPing(2000); // loss

        s.UpdateTransfer(ns, 0.0);
        ns.AddBytesSent(1000);
        ns.AddBytesReceived(2000);
        s.UpdateTransfer(ns, 1.0);

        Assert.True(s.TotalPingPackets > 0);
        Assert.True(s.TransferSamples > 0);

        s.Reset();

        Assert.Equal(0, s.TotalPingPackets);
        Assert.Equal(0, s.LostPingPackets);
        Assert.Equal(0.0, s.PingLossRate);
        Assert.Equal(0, s.MaxLossStreak);
        Assert.Equal(0.0, s.PingMeanMs);
        Assert.Equal(0, s.PingMedianMs);

        Assert.Equal(0, s.TransferSamples);
        Assert.Equal(0.0, s.UploadMeanBps);
        Assert.Equal(0.0, s.DownloadMeanBps);
    }
}