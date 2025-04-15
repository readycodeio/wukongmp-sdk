namespace WukongApi.UI
{
    public class PingIndicatorWidget() : GameWidgetBase(Constants.PingWidgetName)
    {
        public void SetPingText(int pingInMiliseconds)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetPingText {pingInMiliseconds}", true);
        }

        protected override void PostInitialize() { }
    }
}
