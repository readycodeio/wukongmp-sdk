using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;

namespace WukongMp.Api.UI
{
    public class PingIndicatorWidget : GameWidgetBase
    {
        private static PingIndicatorWidget? _instance;
        public static PingIndicatorWidget Instance => _instance ??= new();

        private PingIndicatorWidget() : base(Constants.PingWidgetName) { }

        public void SetPingValue(long pingMs)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetPingValue {pingMs}", true);
        }

        public void SetInfoText(string infoText)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetInfoText {infoText}", true);
        }

        public void ShowInfoText()
        {
            GameWidget?.CallFunctionByNameWithArguments($"ShowInfoText", true);
        }

        public void HideInfoText()
        {
            GameWidget?.CallFunctionByNameWithArguments($"HideInfoText", true);
        }

        private void SetPingText(string pingText)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetPingText {pingText}", true);
        }

        private void SetUnitsText(string pingUnitsText)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetUnitsText {pingUnitsText}", true);
        }

        private void SetStaticTexts(string pingText, string pingUnitsText)
        {
            SetPingText(pingText);
            SetUnitsText(pingUnitsText);
        }

        protected override void PostInitialize() 
        {
            SetStaticTexts(Texts.Ping, Texts.PingUnits);
        }
    }
}