using b1;
using ReadyM.Api.Multiplayer;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct PlayerTransEndData(int unitResId, int unitBornSkillId, bool enbleBlendViewTarget, EPlayerTransEndType transEndType)
{
    public int UnitResId = unitResId;
    public int UnitBornSkillId = unitBornSkillId;
    public bool EnableBlendViewTarget = enbleBlendViewTarget;
    public EPlayerTransEndType TransEndType = transEndType;
}