using System.Threading;
using System.Threading.Tasks;
using B1UI.GSUI;
using CSharpModBase;
using GSE.GSUI;
using UnrealEngine.Runtime;

namespace WukongMp.Api.WukongUtils;

public static class UiUtils
{
    private static CancellationTokenSource _cts = new();
    private static Task? _tipHideTask;

    public static void ShowTip(string tip, bool autoHide)
    {
        Utils.TryRunOnGameThread(() =>
        {
            GenAGPage.ShowPage(39, nameof(ShowTip));
            var dSSimTipsData = new DSSimTipsData(ETipsType.WarnTips, FText.FromString(tip), InShowTime: 5);
            GenACommTips.SetTipsData(dSSimTipsData, nameof(ShowTip));

            if (autoHide)
            {
                if (_tipHideTask is { IsCompleted: false })
                {
                    // cancel previous hide task
                    _cts.Cancel();
                    _cts = new CancellationTokenSource();
                }

                _tipHideTask = Task.Run(async () =>
                {
                    await Task.Delay(5000, _cts.Token);
                    HideTip();
                    _tipHideTask = null;
                }, _cts.Token);
            }
        });
    }

    private static void HideTip()
    {
        Utils.TryRunOnGameThread(() => { GenAGPage.FadeOutPage(39, nameof(ShowTip)); });
    }

    public static void SetHudVisibility(bool visible)
    {
        Utils.TryRunOnGameThread(() => { GenABattleMain.SetBattleMainTempHide(!visible, "TickUpdateUIShowState"); });
    }
}