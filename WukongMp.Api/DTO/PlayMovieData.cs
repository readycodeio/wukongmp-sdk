using b1;
using LiteNetLib.Utils;

namespace WukongMp.Api.DTO;

public readonly struct PlayMovieData(int sequenceID, bool disablePlayerControl, bool disableMovementInput, bool disableLookAtInput, bool hidePlayer, bool hideHud, string overlapBoxGuid, ESequenceBlendInMatchPositionType matchType)
{
    public readonly int SequenceID = sequenceID;
    public readonly bool DisablePlayerControl = disablePlayerControl;
    public readonly bool DisableMovementInput = disableMovementInput;
    public readonly bool DisableLookAtInput = disableLookAtInput;
    public readonly bool HidePlayer = hidePlayer;
    public readonly bool HideHud = hideHud;
    public readonly string OverlapBoxGuid = overlapBoxGuid;
    public readonly ESequenceBlendInMatchPositionType MatchType = matchType;

    public static void Serialize(NetDataWriter outStream, object customObject)
    {
        var data = (PlayMovieData)customObject;
        outStream.Put(data.SequenceID);
        outStream.Put(data.DisablePlayerControl);
        outStream.Put(data.DisableMovementInput);
        outStream.Put(data.DisableLookAtInput);
        outStream.Put(data.HidePlayer);
        outStream.Put(data.HideHud);
        outStream.Put(data.OverlapBoxGuid);
        outStream.Put((byte)data.MatchType);
    }

    public static object Deserialize(NetDataReader inStream)
    {
        var sequenceID = inStream.GetInt();
        var disablePlayerControl = inStream.GetBool();
        var disableMovementInput = inStream.GetBool();
        var disableLookAtInput = inStream.GetBool();
        var hidePlayer = inStream.GetBool();
        var hideHud = inStream.GetBool();
        var overlapBoxGuid = inStream.GetString();
        var matchType = (ESequenceBlendInMatchPositionType)inStream.GetByte();
        return new PlayMovieData(sequenceID, disablePlayerControl, disableMovementInput, disableLookAtInput, hidePlayer, hideHud, overlapBoxGuid, matchType);
    }
}
