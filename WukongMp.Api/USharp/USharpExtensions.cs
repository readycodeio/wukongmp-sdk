using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnrealEngine;
using UnrealEngine.Runtime;

// NOTE: Cannot change the namespace because we need to hide the USharp original extensions within this namespace
// ReSharper disable once CheckNamespace
namespace WukongMp.Api;

public static class USharpExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(this float a, float b, float tolerance)
    {
        return MathF.Abs(a - b) < tolerance;
    }

    public static bool IsNullOrDestroyed([NotNullWhen(false)] this UObject? obj)
    {
        return (object?)obj == null || obj.IsDestroyed || SharedRuntimeState.IsShutdown || obj.HasAnyFlags(EObjectFlags.FinishDestroyed) || obj.IsPendingKill;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ToVector3(this FVector vector)
    {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector ToFVector(this Vector3 vector)
    {
        return new FVector(vector.X, vector.Y, vector.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ToVector3(this FRotator vector)
    {
        return new Vector3(vector.Pitch, vector.Yaw, vector.Roll);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FRotator ToFRotator(this Vector3 vector)
    {
        return new FRotator(vector.X, vector.Y, vector.Z);
    }
}
