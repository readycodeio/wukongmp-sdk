using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct ProjectileSwitchData(
    NetworkId netId,
    string projectileClassName, 
    int bulletSwitchID, 
    int switchIdx) : INetSerializable
{
    public NetworkId NetId = netId;
    public string ProjectileClassName = projectileClassName;
    public int BulletSwitchID = bulletSwitchID;
    public int SwitchIdx = switchIdx;
}
