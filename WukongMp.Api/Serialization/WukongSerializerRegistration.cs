using b1;
using ReadyM.Relay.Common.Serialization;
using ReadyM.Relay.Common.Wukong.ECS.Values;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Values;

namespace WukongMp.Api.Serialization;

public class WukongSerializerRegistration : IRelaySerializerRegistration
{
    public void Register(RelaySerializer serializer)
    {
        serializer.RegisterType(typeof(DamageNumParam), SerializationHelpers.SerializeDamageNumParam, SerializationHelpers.DeserializeDamageNumParam);
        serializer.RegisterType(typeof(EquipmentState), EquipmentState.SerializeUntyped, EquipmentState.DeserializeUntyped);
        serializer.RegisterType(typeof(FRotator), SerializationHelpers.SerializeFRotator, SerializationHelpers.DeserializeFRotator);
        serializer.RegisterType(typeof(FVector), SerializationHelpers.SerializeFVector, SerializationHelpers.DeserializeFVector);
    }
}