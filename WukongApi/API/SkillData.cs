using BtlShare;

namespace WukongApi.API
{
    internal struct SkillData
    {
        public EInputActionType ActionType;
        public int SkillId;
        public int DescId;
        public int ItemId;

        public SkillData(EInputActionType actionType, int skillId, int descId, int itemId)
        {
            ActionType=actionType;
            SkillId=skillId;
            DescId=descId;
            ItemId=itemId;
        }
    }
}
