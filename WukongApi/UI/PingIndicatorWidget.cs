namespace WukongApi.UI
{
    public class PingIndicatorWidget : GameWidgetBase
    {
        public static PingIndicatorWidget Instance { get; } = new();

        private PingIndicatorWidget() : base(Constants.PingWidgetName) { }

        public void SetPingText(int pingMs)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetPingText {pingMs}", true);
        }

        protected override void PostInitialize() { }
    }
}