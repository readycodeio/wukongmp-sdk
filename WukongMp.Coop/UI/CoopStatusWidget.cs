using WukongMp.Api.Resources;
using WukongMp.Api.UI;

namespace WukongMp.Coop.UI;

public sealed class CoopStatusWidget() : GameWidgetBase(CoopStatusWidgetPath)
{
    private const string CoopStatusWidgetPath = "/Game/Mods/WukongMod/WBP_CoopStatus.WBP_CoopStatus_C";

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
        SetConnectedText(BuiltinTexts.Connected);
    }
}