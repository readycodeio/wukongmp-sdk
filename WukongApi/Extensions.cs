using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnrealEngine;
using UnrealEngine.Runtime;

namespace WukongApi
{
    public static class Extensions
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
        public static FQuat ToFQuat(this Quaternion quaternion)
        {
            return new FQuat(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToQuaternion(this FQuat quaternion)
        {
            return new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }
    }
}