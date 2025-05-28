using WukongMp.Api.GameApi.Configuration;
using WukongMp.Api.Old;
using WukongMp.Api.Resources;

namespace WukongMp.Api.UI
{
    public class PingIndicatorWidget : GameWidgetBase
    {
        public static PingIndicatorWidget Instance { get; } = new();

        private PingIndicatorWidget() : base(Constants.PingWidgetName) { }

        public void SetPingValue(int pingMs)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetPingValue {pingMs}", true);
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