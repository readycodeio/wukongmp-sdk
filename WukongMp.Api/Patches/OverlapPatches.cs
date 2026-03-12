using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using b1;
using HarmonyLib;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

internal static class PatchOverlapUtils
{
    internal static readonly object OverlapLock = new();
}

[HarmonyPatch(typeof(BGS_SimpleOverlapMgrSystem), "ThreadFunc")]
[HarmonyPatchCategory(PatchCategory.Global)]
internal static class PatchThreadFunc
{
    public static void Prefix()
    {
        Monitor.Enter(PatchOverlapUtils.OverlapLock);
    }

    public static void Finalizer()
    {
        Monitor.Exit(PatchOverlapUtils.OverlapLock);
    }
}

[HarmonyPatch(typeof(BGS_SimpleOverlapMgrSystem), "OnRegisterEntityUpdatenfo")]
[HarmonyPatchCategory(PatchCategory.Global)]
internal static class PatchOnRegisterEntityUpdatenfo
{
    public static void Prefix()
    {
        Monitor.Enter(PatchOverlapUtils.OverlapLock);
    }

    public static void Finalizer()
    {
        Monitor.Exit(PatchOverlapUtils.OverlapLock);
    }
}

[HarmonyPatch(typeof(BGS_SimpleOverlapMgrSystem), "OnDeregisterEntity")]
[HarmonyPatchCategory(PatchCategory.Global)]
internal static class PatchOnDeregisterEntity
{
    public static void Prefix()
    {
        Monitor.Enter(PatchOverlapUtils.OverlapLock);
    }

    public static void Finalizer()
    {
        Monitor.Exit(PatchOverlapUtils.OverlapLock);
    }
}

[HarmonyPatch(typeof(BGC_SimpleOverlapMgrData), "GetOverlapGridIndexList")]
[HarmonyPatchCategory(PatchCategory.Global)]
internal static class PatchGetOverlapGridIndexList
{
    [ThreadStatic]
    private static List<int>? _list;

    [ThreadStatic]
    private static Func<BGC_SimpleOverlapMgrData, float>? _getter;

    private static bool IsRectangleOverlap(
        FVector2D StartPoint,
        FVector2D EndPoint,
        FVector2D OverlapStartPoint,
        FVector2D OverlapEndPoint)
    {
        return StartPoint.X != (double)EndPoint.X &&
               StartPoint.Y != (double)EndPoint.Y &&
               OverlapStartPoint.X != (double)OverlapEndPoint.X &&
               OverlapStartPoint.Y != (double)OverlapEndPoint.Y &&
               EndPoint.X > (double)OverlapStartPoint.X &&
               StartPoint.X < (double)OverlapEndPoint.X &&
               EndPoint.Y > (double)OverlapStartPoint.Y &&
               StartPoint.Y < (double)OverlapEndPoint.Y;
    }

    public static bool Prefix(
        BGC_SimpleOverlapMgrData __instance,
        FVector2D Location,
        FVector2D SquareSize,
        BGUGridInfo GridInfo,
        out List<int> OutIndexList,
        out bool __result)
    {
        OutIndexList = _list ??= new List<int>();
        OutIndexList.Clear();
        if (_getter == null)
        {
            var getterMethod = __instance.GetType().GetProperty("GridSize", BindingFlags.Instance | BindingFlags.NonPublic)!.GetGetMethod(true);
            _getter = (Func<BGC_SimpleOverlapMgrData, float>)Delegate.CreateDelegate(typeof(Func<BGC_SimpleOverlapMgrData, float>), getterMethod!);
        }

        var gridSize = _getter.Invoke(__instance);
        var StartPoint = GridInfo.CenterLocation - new FVector2D(4.5 * gridSize, 4.5 * gridSize);
        var EndPoint = GridInfo.CenterLocation + new FVector2D(4.5 * gridSize, 4.5 * gridSize);
        var OverlapStartPoint = Location - SquareSize;
        var OverlapEndPoint = Location + SquareSize;
        var num1 = IsRectangleOverlap(StartPoint, EndPoint, OverlapStartPoint, OverlapEndPoint) ? 1 : 0;
        OverlapStartPoint = new FVector2D(FMath.Max(OverlapStartPoint.X, StartPoint.X), FMath.Max(OverlapStartPoint.Y, StartPoint.Y));
        OverlapEndPoint = new FVector2D(FMath.Min(OverlapEndPoint.X, EndPoint.X), FMath.Min(OverlapEndPoint.Y, EndPoint.Y));
        var num2 = (double)FMath.Max(OverlapStartPoint.X, StartPoint.X);
        var num3 = FMath.Min(OverlapEndPoint.X, EndPoint.X);
        var x = (double)GridInfo.CenterLocation.X;
        var num4 = (float)(num2 - x);
        var num5 = num4 % gridSize;
        var num6 = 4 + (int)(num4 / (double)gridSize) + (num4 < 0.0 ? -1 : 1) * (FMath.Abs(num5) > gridSize / 2.0 ? 1 : 0);
        var num7 = num3 - GridInfo.CenterLocation.X;
        var num8 = num7 % gridSize;
        var num9 = 4 + (int)(num7 / (double)gridSize) + (num7 < 0.0 ? -1 : 1) * (FMath.Abs(num8) > gridSize / 2.0 ? 1 : 0);
        var num10 = (double)FMath.Max(OverlapStartPoint.Y, StartPoint.Y);
        var num11 = FMath.Min(OverlapEndPoint.Y, EndPoint.Y);
        var y = (double)GridInfo.CenterLocation.Y;
        var num12 = (float)(num10 - y);
        var num13 = num12 % gridSize;
        var num14 = 4 + (int)(num12 / (double)gridSize) + (num12 < 0.0 ? -1 : 1) * (FMath.Abs(num13) > gridSize / 2.0 ? 1 : 0);
        var num15 = num11 - GridInfo.CenterLocation.Y;
        var num16 = num15 % gridSize;
        var num17 = 4 + (int)(num15 / (double)gridSize) + (num15 < 0.0 ? -1 : 1) * (FMath.Abs(num16) > gridSize / 2.0 ? 1 : 0);
        OutIndexList.Add(num6);
        OutIndexList.Add(num9);
        OutIndexList.Add(num14);
        OutIndexList.Add(num17);
        __result = num1 != 0;
        return false;
    }
}

[HarmonyPatch(typeof(BGC_SimpleOverlapMgrData), "GetSimpleOverlapActorsByMask")]
[HarmonyPatchCategory(PatchCategory.Global)]
internal static class PatchGetSimpleOverlapActorsByMask
{
    public static Exception? Finalizer()
    {
        return null;
    }
}