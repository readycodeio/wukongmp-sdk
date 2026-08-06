using b1;
using ReadyM.Api.Multiplayer.Serialization;
using UnrealEngine.Runtime;

namespace WukongMp.Api.Serialization;

internal class WukongSerializerRegistration : IRelaySerializerRegistration
{
    public void Register(RelaySerializer serializer)
    {
        serializer.RegisterType(typeof(DamageNumParam), SerializationHelpers.SerializeDamageNumParam, SerializationHelpers.DeserializeDamageNumParam);
        serializer.RegisterType(typeof(FRotator), SerializationHelpers.SerializeFRotator, SerializationHelpers.DeserializeFRotator);
        serializer.RegisterType(typeof(FVector), SerializationHelpers.SerializeFVector, SerializationHelpers.DeserializeFVector);
    }
}