using UnrealEngine.Engine;

namespace WukongMp.Api.Old.State
{
    public struct MontageState
    {
        public UAnimMontage? LocalMontage { get; set; }
        public float LocalMontagePosition { get; set; }
        public UAnimInstance? LocalAnimationInstance { get; set; }
    }
}
