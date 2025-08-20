using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct ProjectileSwitchData(string projectileClassName, int bulletSwitchID, int switchIdx) : INetSerializable
{
    public string ProjectileClassName = projectileClassName;
    public int BulletSwitchID = bulletSwitchID;
    public int SwitchIdx = switchIdx;
}
