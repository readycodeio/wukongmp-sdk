using WukongMp.Api.Resources;

namespace WukongMp.Api.UI
{
    public class FreeCameraControlsWidget : GameWidgetBase
    {
        private const string FreeCameraWidgetPath = "/Game/Mods/WukongMod/WBP_FreeCameraControls.WBP_FreeCameraControls_C";

        public FreeCameraControlsWidget() : base(FreeCameraWidgetPath) { }

        public void SetDownDescriptionText(string down)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetDownDescriptionText {down}", true);
        }

        public void SetMoveDescriptionText(string move)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetMoveDescriptionText {move}", true);
        }

        public void SetRotateDescriptionText(string rotate)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetRotateDescriptionText {rotate}", true);
        }

        public void SetUpDescriptionText(string up)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetUpDescriptionText {up}", true);
        }

        public void SetDownControlsText(string down)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetDownControlsText {down}", true);
        }

        public void SetMoveControlsText(string move)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetMoveControlsText {move}", true);
        }

        public void SetRotateControlsText(string rotate)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetRotateControlsText {rotate}", true);
        }

        public void SetUpControlsText(string up)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetUpControlsText {up}", true);
        }

        private void SetStaticTexts(
            string downControls, string downDescription,
            string moveControls, string moveDescription,
            string rotateControls, string rotateDescription,
            string upControls, string upDescription)
        {
            SetDownControlsText(downControls);
            SetDownDescriptionText(downDescription);
            SetMoveControlsText(moveControls);
            SetMoveDescriptionText(moveDescription);
            SetRotateControlsText(rotateControls);
            SetRotateDescriptionText(rotateDescription);
            SetUpControlsText(upControls);
            SetUpDescriptionText(upDescription);
        }

        protected override void PostInitialize()
        {
            SetStaticTexts(
                Texts.CameraDownControls, Texts.CameraDownDescription,
                Texts.CameraMoveControls, Texts.CameraMoveDescription,
                Texts.CameraRotateControls, Texts.CameraRotateDescription,
                Texts.CameraUpControls, Texts.CameraUpDescription);
        }
    }
}