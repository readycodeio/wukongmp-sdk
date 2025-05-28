using WukongMp.Api.GameApi.Configuration;
using WukongMp.Api.Old;
using WukongMp.Api.Resources;

namespace WukongMp.Api.UI
{
    public class CoopStatusWidget : GameWidgetBase
    {
        public CoopStatusWidget() : base(Constants.CoopStatusWidgetName) { }

        public void SetConnectedCount(int count)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetConnectedCount {count}", true);
        }

        public void SetMaxConnectedCount(int count)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetMaxConnectedCount {count}", true);
        }

        public void AddPlayer(string playerName)
        {
            GameWidget?.CallFunctionByNameWithArguments($"AddPlayer {playerName}", true);
        }

        public void RemovePlayer(string playerName)
        {
            GameWidget?.CallFunctionByNameWithArguments($"RemovePlayer {playerName}", true);
        }

        private void SetConnectedText(string connected)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetConnectedText {connected}", true);
        }

        protected override void PostInitialize()
        {
            SetConnectedText(Texts.Connected);
        }
    }
}
