using b1;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS.Values;

namespace WukongMp.Api.ECS.Components;

public struct LocalMainCharacterComponent : IComponent
{
    private BGUCharacterCS? _pawn;

    public bool IsPlayerSynced;

    public bool IsSpectatorLocally;
    
    [Ignore]
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

    public bool HasPawn => !_pawn.IsNullOrDestroyed();
        
    public bool IsRespawning { get; set; }
    public bool RunImmobilizePatches { get; set; }
    public MontageState MontageState { get; set; }
        
    public bool ReceivedPhantomRushExit { get; set; }
    public int TeleportFinishFrames { get; set; }
    public float AIPathMoveStuckTimer { get; set; }
    public bool IsAIPathMoveStuck { get; set; }

    // FIXME: Move to PlayerComponent?
    public bool IsWaitingForSequence { get; set; }
    public bool IsJoiningSequence { get; set; }
    public FVector JoiningSequenceLocation { get; set; }
    public int LastSyncableSequenceId { get; set; }

    private AActor? _markerActor;
    
    [Ignore]
    public AActor? MarkerActor
    {
        get
        {
            if (_markerActor != null && _markerActor.IsNullOrDestroyed())
            {
                return null;
            }

            return _markerActor;
        }
        set => _markerActor = value;
    }
}