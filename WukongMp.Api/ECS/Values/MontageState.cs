using Friflo.Json.Fliox;
using UnrealEngine.Engine;

namespace WukongMp.Api.ECS.Values;

public struct MontageState
{
    [Ignore]
    public UAnimMontage? LocalMontage { get; set; }
    public float LocalMontagePosition { get; set; }
    [Ignore]
    public UAnimInstance? LocalAnimationInstance { get; set; }
}