using b1;
using ReadyM.Api.Multiplayer;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct PlayerTransBeginData(int unitResId, int unitBornSkillId, bool enbleBlendViewTarget, EPlayerTransBeginType transBeginType)
{
    public int UnitResId = unitResId;
    public int UnitBornSkillId = unitBornSkillId;
    public bool EnableBlendViewTarget = enbleBlendViewTarget;
    public EPlayerTransBeginType TransBeginType = transBeginType;
}