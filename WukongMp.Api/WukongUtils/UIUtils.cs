using B1UI.GSUI;
using GSE.GSUI;
using UnrealEngine.Runtime;

namespace WukongMp.Api.WukongUtils
{
    public static class UiUtils
    {
        public static void ShowTip(string tip, bool autoHide)
        {
            GenAGPage.ShowPage(39, nameof(ShowTip));
            var dSSimTipsData = new DSSimTipsData(ETipsType.WarnTips, FText.FromString(tip), InIsCloseAutoHide: autoHide, 5);
            GenACommTips.SetTipsData(dSSimTipsData, nameof(ShowTip));
        }

        public static void HideTip()
        {
            GenAGPage.HidePage(39, nameof(ShowTip));
        }

        public static void SetHudVisibility(bool visible)
        {
            GenABattleMain.SetBattleMainTempHide(!visible, "TickUpdateUIShowState");
        }
    }
}
