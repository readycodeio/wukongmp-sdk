using b1;
using ReadyM.Api.Serialization;
using UnrealEngine.Runtime;
using WukongMp.Api.Old.State;

namespace WukongMp.Api;

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