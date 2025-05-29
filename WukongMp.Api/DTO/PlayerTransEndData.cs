using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
public partial struct PlayerTransEndData(int unitResId, int unitBornSkillId, bool enbleBlendViewTarget, EPlayerTransEndType transEndType) : INetSerializable
{
    public int UnitResId = unitResId;
    public int UnitBornSkillId = unitBornSkillId;
    public bool EnableBlendViewTarget = enbleBlendViewTarget;
    public EPlayerTransEndType TransEndType = transEndType;
}