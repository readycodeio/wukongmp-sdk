using b1;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct ProjectileDeadData(string projectileClassName, EBGUBulletDestroyReason reason) : INetSerializable
{
    public string ProjectileClassName = projectileClassName;
    public EBGUBulletDestroyReason Reason = reason;
}
