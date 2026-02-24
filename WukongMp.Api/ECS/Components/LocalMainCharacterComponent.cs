using b1;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Values;

namespace WukongMp.Api.ECS.Components;

public struct LocalMainCharacterComponent : IComponent
{
    public bool IsPlayerSynced;

    public bool IsSpectatorLocally;
    public bool ShouldDisableCollision;

    [Ignore]
    public BGUCharacterCS? LastPawn { get; set; }

    public bool IsRespawning { get; set; }
    public bool ReceivedPhantomRushExit { get; set; }
    public bool RunImmobilizePatches { get; set; }
    public MontageStateData MontageState { get; set; }

    public int TeleportFinishFrames { get; set; }

    // FIXME: Move to PlayerComponent?
    public bool IsWaitingForSequence { get; set; }
    public bool IsJoiningSequence { get; set; }
    public FVector JoiningSequenceLocation { get; set; }
    public bool IsInSequence { get; set; }

    // Cheat parameters
    public bool InstantSkillCooldown { get; set; }
    public bool HasInfiniteMana { get; set; }
    public bool HasInfiniteVessel { get; set; }
    public bool HasInfiniteTransform { get; set; }
    public bool SpiritCooldownEnabled { get; set; }
    public float SpiritCooldownTime { get; set; }
    public bool ShouldSetSpiritCooldown { get; set; }

    // Dead animation timer
    public bool IsDuringDeathAnim { get; set; }
    public float DeadAnimationTime { get; set; }

    [Ignore]
    public AActor? MarkerActor
    {
        get
        {
            if (field != null && field.IsNullOrDestroyed())
            {
                return null;
            }

            return field;
        }
        set;
    }
}