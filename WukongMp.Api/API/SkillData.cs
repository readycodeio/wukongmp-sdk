using BtlShare;

namespace WukongMp.Api.API
{
    internal struct SkillData(EInputActionType actionType, int skillId, int descId, int itemId)
    {
        public readonly EInputActionType ActionType = actionType;
        public readonly int SkillId = skillId;
        public readonly int DescId = descId;
        public int ItemId = itemId;
    }
}