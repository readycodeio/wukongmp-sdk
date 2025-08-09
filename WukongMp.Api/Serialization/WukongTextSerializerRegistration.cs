using b1;
using ReadyM.Relay.Common.Serialization;
using ReadyM.Relay.Common.Wukong.ECS.Values;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Values;

namespace WukongMp.Api.Serialization;

public class WukongTextSerializerRegistration : ITextRelaySerializerRegistration
{
    public void Register(TextRelaySerializer serializer)
    {
        serializer.RegisterPolymorphicType("dataNumParam", typeof(DamageNumParam), TextSerializationHelpers.TextSerializeDamageNumParam, TextSerializationHelpers.TextDeserializeDamageNumParam);
        serializer.RegisterPolymorphicType("equipmentState", EquipmentState.TextSerialize, EquipmentState.TextDeserialize);
        serializer.RegisterPolymorphicType("fRotator", typeof(FRotator), TextSerializationHelpers.TextSerializeFRotator, TextSerializationHelpers.TextDeserializeFRotator);
        serializer.RegisterPolymorphicType("fVector", typeof(FVector), TextSerializationHelpers.TextSerializeFVector, TextSerializationHelpers.TextDeserializeFVector);
    }
}