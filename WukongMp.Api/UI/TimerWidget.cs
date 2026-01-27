namespace WukongMp.Api.UI
{
    public class TimerWidget : GameWidgetBase
    {
        private const string TimerWidgetPath = "/Game/Mods/WukongMod/WBP_Timer.WBP_Timer_C";

        public TimerWidget() : base(TimerWidgetPath)
        {
        }

        public void SetText(int minutes, int seconds)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetText {minutes} {seconds}", true);
        }

        protected override void PostInitialize() { }
    }
}
