using b1;
using ReadyM.Relay.Common;
using UnrealEngine.Runtime;
using WukongMp.Api.Old.State;

namespace WukongMp.Api;

public class WukongSerializerRegistration : IRelaySerializerRegistration
{
    public void Register(RelaySerializer serializer)
    {
        serializer.RegisterType(typeof(DamageNumParam), SerializationHelpers.SerializeDamageNumParam, SerializationHelpers.DeserializeDamageNumParam);
        serializer.RegisterType(typeof(EquipmentState), EquipmentState.Serialize, EquipmentState.Deserialize);
        serializer.RegisterType(typeof(FRotator), SerializationHelpers.SerializeFRotator, SerializationHelpers.DeserializeFRotator);
        serializer.RegisterType(typeof(FVector), SerializationHelpers.SerializeFVector, SerializationHelpers.DeserializeFVector);
    }
}