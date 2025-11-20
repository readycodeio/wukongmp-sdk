using b1;
using Friflo.Engine.ECS.Systems;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class MoveMainCharacterSystem(WukongPlayerState playerState) : QuerySystem<LocalMainCharacterComponent, MainCharacterComponent>
{
    private int _frameCount = 0;
    private readonly int _forceSetFrameNumber = 100;
    private readonly float _totalTime = Constants.ToleratedLatencyMs / 1000f;

    protected override void OnUpdate()
    {
        Query.ForEachEntity((ref localMainComp, ref mainComp, _) =>
        {
            if (mainComp.PlayerId == playerState.LocalPlayerId)
                return;

            if (!localMainComp.HasPawn)
                return;

            _frameCount++;
            InterpolationMove(ref localMainComp, ref mainComp, Tick.deltaTime, _frameCount >= _forceSetFrameNumber);
            if (_frameCount >= _forceSetFrameNumber)
            {
                _frameCount = 0;
            }
        });
    }

    private void InterpolationMove(ref LocalMainCharacterComponent localMainComp, ref MainCharacterComponent mainCharacterComp, float deltaTime, bool forceSet)
    {
        var pawn = localMainComp.Pawn;

        FRotator currentRotation = BGUFuncLibActorTransformCS.BGUGetActorRotation(pawn);
        FVector currentLocation = BGUFuncLibActorTransformCS.BGUGetActorLocation(pawn);

        FRotator targetRotation = mainCharacterComp.Rotation.ToFRotator();
        FVector targetLocation = mainCharacterComp.Location.ToFVector();

        bool updateLocation = !currentLocation.Equals(targetLocation, Constants.FloatComparisonTolerance);
        bool updateRotation = !currentRotation.Equals(targetRotation, Constants.FloatComparisonTolerance);

        if (updateRotation)
        {
            FVector currentForwardVector = BGUFuncLibActorTransformCS.BGUGetActorForwardVector(pawn);
            FVector2D unitRotateAimDir = new FVector2D(currentForwardVector);
            FVector2D unit2TargetDir = new FVector2D(targetRotation.Vector().GetSafeNormal());
            float rotateAngle2D = BGU_MoveUtil.GetRotateAngle2D(unitRotateAimDir, unit2TargetDir);
            FRotator newRotation = currentRotation;
            if (BGU_MoveUtil.IsRotateClockwise(unitRotateAimDir, unit2TargetDir))
            {
                newRotation.Yaw = MathLib.NormalizeAxis(newRotation.Yaw + rotateAngle2D);
            }
            else
            {
                newRotation.Yaw = MathLib.NormalizeAxis(newRotation.Yaw - rotateAngle2D);
            }
            if (!forceSet)
            {
                float interpSpeed = rotateAngle2D / _totalTime;
                newRotation = MathLib.RInterpConstantTo(in currentRotation, in newRotation, deltaTime, interpSpeed);
            }
            BGUFuncLibActorTransformCS.BGUSetActorRotation(pawn, newRotation, bTeleportPhysics: false, false);
        }

        if (updateLocation)
        {
            FVector newLocation = targetLocation;
            if (!forceSet)
            {
                float interpSpeed2 = FVector.Dist(currentLocation, targetLocation) / _totalTime;
                newLocation = MathLib.VInterpConstantTo(in currentLocation, in targetLocation, deltaTime, interpSpeed2);
            }
            BGUFuncLibActorTransformCS.BGUSetActorLocation(pawn, newLocation, bSweep: false, bTeleport: false, NeedReturnHitResult: false, false);
        }
    }
}
