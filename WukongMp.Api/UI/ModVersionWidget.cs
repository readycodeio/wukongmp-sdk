namespace WukongMp.Api.UI
{
    public class ModVersionWidget : GameWidgetBase
    {
        private const string ModVersionWidgetPath = "/Game/Mods/CustomLuaMod/WBP_ModVersion.WBP_ModVersion_C";

        public ModVersionWidget() : base(ModVersionWidgetPath) { }

        public void SetVersionText(string version)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetVersionText {version}", true);
        }

        protected override void PostInitialize() { }
    }
}
