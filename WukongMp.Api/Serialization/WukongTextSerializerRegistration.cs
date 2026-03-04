using ReadyM.Api.Multiplayer.Serialization;
using ReadyM.Wukong.Common.ECS.Values;

namespace WukongMp.Api.Serialization;

public class WukongTextSerializerRegistration : ITextRelaySerializerRegistration
{
    public void Register(TextRelaySerializer serializer)
    {
        serializer.RegisterPolymorphicType("dataNumParam", TextSerializationHelpers.TextSerializeDamageNumParam, TextSerializationHelpers.TextDeserializeDamageNumParam);
        serializer.RegisterPolymorphicType("equipmentState", EquipmentState.TextSerialize, EquipmentState.TextDeserialize);
        serializer.RegisterPolymorphicType("fRotator", TextSerializationHelpers.TextSerializeFRotator, TextSerializationHelpers.TextDeserializeFRotator);
        serializer.RegisterPolymorphicType("fVector", TextSerializationHelpers.TextSerializeFVector, TextSerializationHelpers.TextDeserializeFVector);
    }
}