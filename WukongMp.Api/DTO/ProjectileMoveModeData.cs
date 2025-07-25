using BtlShare;
using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct ProjectileMoveModeData(string projectileClassName, EBulletOrMagicFieldMoveModeType moveMode) : INetSerializable
{
    public string ProjectileClassName = projectileClassName;
    public EBulletOrMagicFieldMoveModeType MoveMode = moveMode;
}
