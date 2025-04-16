using UnrealEngine.Engine;

namespace WukongApi.State
{
    public struct MontageState
    {
        public UAnimMontage? LocalMontage { get; set; }
        public float LocalMontagePosition { get; set; }
        public UAnimInstance? LocalAnimationInstance { get; set; }
    }
}
