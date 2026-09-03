using b1;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Values;

namespace WukongMp.Api.ECS.Components;

internal struct LocalMainCharacterComponent : IComponent
{
    public bool IsPlayerSynced;

    public bool IsSpectatorLocally;
    public bool ShouldDisableCollision;

    [Ignore]
    public BGUCharacterCS? LastPawn { get; set; }

    public bool IsRespawning { get; set; }
    public MontageStateData MontageState { get; set; }
    public int TeleportFinishFrames { get; set; }

    // FIXME: Move to PlayerComponent?
    public bool IsWaitingForSequence { get; set; }
    public bool IsJoiningSequence { get; set; }
    public FVector JoiningSequenceLocation { get; set; }
    public bool IsInSequence { get; set; }

    // Dead animation timer
    public bool IsDuringDeathAnim { get; set; }
    public float DeadAnimationTime { get; set; }
}