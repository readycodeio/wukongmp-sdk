using WukongMp.Api.Configuration;

namespace WukongMp.Api.UI
{
    public class GameMessageWidget : GameWidgetBase
    {       
        public GameMessageWidget() : base(Constants.GameMessageWidgetName) { }

        public void SetMainText(string message)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetMainText {message}", true);
        }

        public void SetSecondText(string message)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetSecondText {message}", true);
        }

        public void SetThirdText(string message)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetThirdText {message}", true);
        }

        public void ClearMessages()
        {
            SetMainText("");
            SetSecondText("");
            SetThirdText("");
        }

        protected override void PostInitialize() { }
    }
}