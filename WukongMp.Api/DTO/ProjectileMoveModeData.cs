using BtlShare;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
internal partial struct ProjectileMoveModeData(NetworkId netId, string projectileClassName, EBulletOrMagicFieldMoveModeType moveMode) : INetSerializable
{
    public NetworkId NetId = netId;
    public string ProjectileClassName = projectileClassName;
    public EBulletOrMagicFieldMoveModeType MoveMode = moveMode;
}
