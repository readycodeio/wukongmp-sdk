using b1;
using Friflo.Engine.ECS;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Values;

namespace WukongMp.Api.ECS.Components;

public struct LocalMainCharacterComponent : IComponent
{
    private BGUCharacterCS? _pawn;

    public bool IsPlayerSynced;
    
    public BGUCharacterCS? Pawn
    {
        get
        {
            if (!IsPlayerSynced)
            {
                return null;
            }
            
            if (_pawn.IsNullOrDestroyed())
            {
                Logging.LogWarning("Player pawn is null or destroyed");
                return null;
            }

            return _pawn;
        }
        set => _pawn = value;
    }

    public bool HasPawn
        => !_pawn.IsNullOrDestroyed();
        
    public bool RunImmobilizePatches { get; set; }
    public MontageState MontageState { get; set; }
        
    public bool ReceivedPhantomRushExit { get; set; }
    public int TeleportFinishFrames { get; set; }
    public float AIPathMoveStuckTimer { get; set; }
    public bool IsAIPathMoveStuck { get; set; }

    // FIXME: Move to PlayerComponent?
    public bool IsWaitingForSequence { get; set; }
    public bool IsJoiningSequence { get; set; }
    public FVector SequenceLocation { get; set; }
    public int WaitingSequenceId { get; set; }
    
    private AActor? _markerActor;
    
    public AActor? MarkerActor
    {
        get
        {
            if (_markerActor != null && _markerActor.IsNullOrDestroyed())
            {
                Logging.LogTrace("Marker actor is destroyed");
                return null;
            }

            return _markerActor;
        }
        set => _markerActor = value;
    }
        
    public void UpdateMarkerPosition()
    {
        if (MarkerActor != null)
        {
            if (Pawn == null)
            {
                Logging.LogError("Pawn is null");
                return;
            }

            var markerHeight = Pawn.CapsuleComponent.GetScaledCapsuleHalfHeight() * 1.1;
            MarkerActor.SetActorLocation(Pawn.GetActorLocation() + new FVector(0, 0, markerHeight), false, out _, true);
        }
    }
}