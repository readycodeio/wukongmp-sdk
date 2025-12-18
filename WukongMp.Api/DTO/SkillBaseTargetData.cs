using System.Numerics;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Serialization;

namespace WukongMp.Api.DTO;

[DeriveINetSerializable]
[DeriveJsonSerializable]
public partial struct SkillBaseTargetData(NetworkId character, NetworkId target, Vector3 location, byte sourceType, string sceneCompName) : INetSerializable
{
    public NetworkId Character = character;
    public NetworkId Target = target;
    public Vector3 Location = location;
    public byte SourceType = sourceType;
    public string SceneCompName = sceneCompName;
}