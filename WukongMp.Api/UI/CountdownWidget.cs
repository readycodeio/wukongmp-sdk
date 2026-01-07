namespace WukongMp.Api.UI
{
    public class CountdownWidget : GameWidgetBase
    {
        private const string CountdownWidgetPath = "/Game/Mods/CustomLuaMod/WBP_Countdown.WBP_Countdown_C";

        public CountdownWidget() : base(CountdownWidgetPath) { }

        public void SetText(int seconds)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetText {seconds}", true);
        }

        protected override void PostInitialize() { }
    }
}