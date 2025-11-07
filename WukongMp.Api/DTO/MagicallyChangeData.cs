using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct MagicallyChangeData(string configAssetName, bool compressed, int skillID, int recoverSkillID, ECastReason_MagicallyChange castReason) : INetSerializable
{
    public string ConfigAssetName = configAssetName;
    public int SkillID = skillID;
    public int RecoverSkillID = recoverSkillID;
    public ECastReason_MagicallyChange CastReason = castReason;
    public bool Compressed = compressed;
}
