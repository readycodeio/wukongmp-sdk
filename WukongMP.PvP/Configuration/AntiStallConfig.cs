namespace WukongMp.PvP.Configuration
{
    public class AntiStallConfig
    {
        public const int WarningDuration = 6; // seconds
        public const float ActiveDuration = 3f; // seconds

        public const float RoomEngagementThreshold = 1f;
        public const float MaxRoomEngagementScore = 300f;
        public const float RoomEngagementDecayScore = 20f;
        public const float DamageRoomEngagementScore = 100f;
        public const float AttackRoomEngagementScore = 40f;

        public const float BaseAttributeDecayRate = 0.7f;
        public const float AttributeDecayMultiplier = 0.5f;

        public const float PlayerEngagementMultiplierIncrease = 0.15f;
        public const float PlayerEngagementMultiplierDecay = 0.1f;
        public const float PlayerEngagementMultiplierMax = 1.5f;
        public const float PlayerEngagementMultiplierMin = 0.5f;

        public const float PlayersFacingThreshold = 0.7f;
    }
}
