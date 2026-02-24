using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct MagicallyChangeData(NetworkId netId, string configAssetName, bool compressed, int skillID, int recoverSkillID, int curVigorSkillID, ECastReason_MagicallyChange castReason) : INetSerializable
{
    public NetworkId NetId = netId;
    public string ConfigAssetName = configAssetName;
    public int SkillID = skillID;
    public int RecoverSkillID = recoverSkillID;
    public int CurVigorSkillID = curVigorSkillID;
    public ECastReason_MagicallyChange CastReason = castReason;
    public bool Compressed = compressed;
}
