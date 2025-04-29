namespace WukongApi.UI
{
    public class PingIndicatorWidget : GameWidgetBase
    {
        public static PingIndicatorWidget Instance { get; } = new();

        private PingIndicatorWidget() : base(Constants.PingWidgetName) { }

        public void SetPingValue(int pingMs)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetPingValue {pingMs}", true);
        }

        public void SetDescriptionTexts(string pingText, string pingUnitsText)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetDescriptionTexts {pingText} {pingUnitsText}", true);
        }

        protected override void PostInitialize() 
        {
            SetDescriptionTexts(Texts.PingString, Texts.PingUnitsString);
        }
    }
}