using System.Threading;
using System.Threading.Tasks;
using CSharpModBase;
using Friflo.Engine.ECS;
using ReadyM.Relay.Common;
using ReadyM.Relay.Common.Wukong.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.Old;
using WukongMp.Api.Resources;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

public partial class WukongMpMod : WukongMpModBase
{
    public static WukongMpMod Instance { get; } = new();

    private WukongMpMod() { }

    public override void Initialize()
    {
        base.Initialize();

        WukongMP.Instance.ConfigureEventCallbacks();
    }

    protected override void Patch()
    {
        base.Patch();

        const string category = Constants.IsCoop ? Constants.CoopPatches : Constants.PvpPatches;
        Harmony.PatchCategory(category);
        Logging.LogInformation("Patched Harmony WukongMpMod {Patch}", category);
    }
    
    protected override void Unpatch()
    {
        base.Unpatch();

        const string category = Constants.IsCoop ? Constants.CoopPatches : Constants.PvpPatches;
        Harmony.UnpatchCategory(category);
        Logging.LogInformation("Unpatched Harmony WukongMpMod {Patch}", category);
    }

    protected override void OnPingUpdated(int ping)
    {
        PingIndicatorWidget.Instance.SetPingValue(ping);
    }

    // TODO: After we remove the Client, this will be removed as well, leaving just the call to Tick()
    public void RunEcsWorldUpdate()
    {
        Client.SetCachedPlayerProperties();
        Tick(default);
    }

    public void SetMonsterHpScaling(int scaling)
    {
        if (!IsMasterClient)
        {
            UIUtils.ShowTip(string.Format(Texts.OnlyRoomOwnerCanUse, "/hp_scaling"));
        }

        Logging.LogDebug("Setting monster HP scaling to {Scaling}x", scaling);

        World.Query<HpComponent, LocalTamerComponent>().Each(new ScaleMonsterHpJob(scaling));
    }

    public Task<bool> UploadWorldSaveAsync(byte[] content, CancellationToken ct = default)
        => Blobs.UploadBlobAsync(new BlobInfo(Constants.CoopWorldArchiveName, content), ct);

    public Task<BlobInfo?> DownloadWorldSaveAsync(CancellationToken ct = default) => Blobs.DownloadBlobAsync(Constants.CoopWorldArchiveName, ct);

    public Task<bool> UploadPlayerSaveAsync(byte[] content, CancellationToken ct = default)
        => Blobs.UploadBlobAsync(new BlobInfo(PlayerSaveName, content), ct);

    public Task<BlobInfo?> DownloadPlayerSaveAsync(CancellationToken ct = default) => Blobs.DownloadBlobAsync(PlayerSaveName, ct);

    private static string PlayerSaveName => $"player_{CmdLineParams.Instance.UserGuid:N}.sav";
}