namespace WukongApi.UI
{
    public class PingIndicatorWidget : GameWidgetBase
    {
        public static PingIndicatorWidget Instance { get; } = new();

        private PingIndicatorWidget() : base(Constants.PingWidgetName) { }
        
        public void SetPingText(int pingInMiliseconds)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetPingText {pingInMiliseconds}", true);
        }

        protected override void PostInitialize() { }
    }
}
