using System;
using System.Diagnostics.CodeAnalysis;
using UnrealEngine;
using UnrealEngine.Runtime;

namespace WukongApi
{
    public static class Extensions
    {
        public static bool Equals(this float a, float b, float tolerance)
        {
            return MathF.Abs(a - b) < tolerance;
        }

        public static bool IsNullOrDestroyed([NotNullWhen(false)] this UObject? obj)
        {
            return (object?)obj == null || obj.IsDestroyed || SharedRuntimeState.IsShutdown || obj.HasAnyFlags(EObjectFlags.FinishDestroyed) || obj.IsPendingKill;
        }
    }
}