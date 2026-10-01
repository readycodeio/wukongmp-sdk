using System;
using b1;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Client.Entities;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk.Archetypes.Mixins;

namespace WukongMp.Sdk.Services;

[Service]
public sealed partial class MarkerDisplay(IEntities entities)
{
    private void Update()
    {
        var localPlayerController = GameUtils.GetPlayerController();
        if (localPlayerController == null)
            return;

        var viewTarget = localPlayerController.GetViewTarget();
        var viewTargetLocation = viewTarget.GetActorLocation();

        foreach (var (marker, character) in entities.Query<Marker, MappedCharacter>())
        {
            UpdateMarkerLocation(character.Pawn, marker.MarkerActor, viewTargetLocation);
        }

        foreach (var (marker, tamer) in entities.Query<Marker, MappedTamer>())
        {
            UpdateMarkerLocation(tamer.Pawn, marker.MarkerActor, viewTargetLocation);
        }
    }

    private static void UpdateMarkerLocation(BGUCharacterCS? pawn, AActor? marker, FVector viewTargetLocation)
    {
        if (USharpExtensions.IsNullOrDestroyed(marker) || USharpExtensions.IsNullOrDestroyed(pawn))
            return;

        var location = pawn.GetActorLocation();
        var distance = FVector.Dist2D(viewTargetLocation, location);
        var coefficient = Math.Min(distance / Constants.MaxMarkerHeightDistance, 1);
        var markerHeight = pawn.CapsuleComponent.GetScaledCapsuleHalfHeight() * (1 + Constants.BaseMarkerHeightCoefficient + coefficient);
        marker.SetActorLocation(location + new FVector(0, 0, markerHeight), false, out _, true);
    }
}