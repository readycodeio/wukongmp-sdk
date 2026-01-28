namespace WukongMp.PvP.Configuration
{
    public class AntiStallConfig
    {
        public const int WarningDuration = 6; // seconds
        public const float ActiveDuration = 3f; // seconds

        public const float EngagementThreshold = 1f;
        public const float MaxEngagementScore = 300f;
        public const float EngagementDecayScore = 20f; // per second
        public const float DamageEngagementScore = 100f;
        public const float AttackEngagementScore = 40f;

        public const float BaseDecayRate = 0.8f;
        public const float DecayMultiplier = 0.8f;
    }
}
