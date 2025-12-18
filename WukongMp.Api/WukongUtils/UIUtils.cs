using System.Threading;
using System.Threading.Tasks;
using B1UI.GSUI;
using CSharpModBase;
using GSE.GSUI;
using UnrealEngine.Runtime;

namespace WukongMp.Api.WukongUtils;

public static class UiUtils
{
    public static void ShowTip(string tip, bool autoHide)
    {
        Utils.TryRunOnGameThread(() =>
        {
            GenAGPage.ShowPage(39, nameof(ShowTip));
            var dSSimTipsData = new DSSimTipsData(ETipsType.WarnTips, FText.FromString(tip), InIsCloseAutoHide: !autoHide, InShowTime: 5);
            GenACommTips.SetTipsData(dSSimTipsData, nameof(ShowTip));
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