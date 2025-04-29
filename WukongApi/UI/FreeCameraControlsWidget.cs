namespace WukongApi.UI
{
    public class FreeCameraControlsWidget : GameWidgetBase
    {
        public static FreeCameraControlsWidget Instance { get; } = new();

        private FreeCameraControlsWidget() : base(Constants.FreeCameraWidgetName) { }

        public void SetDescriptionTexts(string down, string move, string rotate, string up)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetDescriptionTexts {down} {move} {rotate} {up}", true);
        }

        public void SetControlsTexts(string down, string move, string rotate, string up)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetControlsTexts {down} {move} {rotate} {up}", true);
        }

        protected override void PostInitialize()
        {
            SetDescriptionTexts(Texts.CameraDownDescription, Texts.CameraMoveDescription, Texts.CameraRotateDescription, Texts.CameraUpDescription);
            SetControlsTexts(Texts.CameraDownControls, Texts.CameraMoveControls, Texts.CameraRotateControls, Texts.CameraUpControls);
        }
    }
}