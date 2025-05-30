using B1UI.GSUI;
using CSharpModBase;
using GSE.GSUI;
using UnrealEngine.Runtime;

namespace WukongMp.Api.WukongUtils
{
    public static class UIUtils
    {
        public static void ShowTip(string tip)
        {
            Utils.TryRunOnGameThread(() =>
            {
                GenAGPage.ShowPage(39, nameof(ShowTip));
                var dSSimTipsData = new DSSimTipsData(ETipsType.WarnTips, FText.FromString(tip), InIsCloseAutoHide: false, 5);
                GenACommTips.SetTipsData(dSSimTipsData, nameof(ShowTip));
            });
        }

        public static void HideTip()
        {
            Utils.TryRunOnGameThread(() =>
            {
                GenAGPage.HidePage(39, nameof(ShowTip));
            });
        }

        public static void SetHudVisibility(bool visible)
        {
            GenABattleMain.SetBattleMainTempHide(!visible, "TickUpdateUIShowState");
        }
    }
}
