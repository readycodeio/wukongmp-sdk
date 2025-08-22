using System;
using System.Collections.Generic;
using b1;
using HarmonyLib;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BGC_SimpleOverlapMgrData), "GetOverlapGridIndexList")]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public static class PatchGetOverlapGridIndexList
{
    [ThreadStatic] private static List<int>? _list;

    private static bool IsRectangleOverlap(
        FVector2D StartPoint,
        FVector2D EndPoint,
        FVector2D OverlapStartPoint,
        FVector2D OverlapEndPoint)
    {
        return (double) StartPoint.X != (double) EndPoint.X && 
               (double) StartPoint.Y != (double) EndPoint.Y &&
               (double) OverlapStartPoint.X != (double) OverlapEndPoint.X && 
               (double) OverlapStartPoint.Y != (double) OverlapEndPoint.Y && 
               (double) EndPoint.X > (double) OverlapStartPoint.X && 
               (double) StartPoint.X < (double) OverlapEndPoint.X &&
               (double) EndPoint.Y > (double) OverlapStartPoint.Y && 
               (double) StartPoint.Y < (double) OverlapEndPoint.Y;
    }

    public static bool Prefix(
        BGC_SimpleOverlapMgrData __instance,
        FVector2D Location,
        FVector2D SquareSize,
        BGUGridInfo GridInfo,
        out List<int> OutIndexList)
    {
        OutIndexList = _list ??= new List<int>();
        OutIndexList.Clear();
        var gridSize =
            AccessTools.FieldRefAccess<BGC_SimpleOverlapMgrData, float>(__instance,
                "<GridSize>k__BackingField");
        var StartPoint = GridInfo.CenterLocation - new FVector2D(4.5 * (double)gridSize, 4.5 * (double)gridSize);
        var EndPoint = GridInfo.CenterLocation + new FVector2D(4.5 * (double)gridSize, 4.5 * (double)gridSize);
        var OverlapStartPoint = Location - SquareSize;
        var OverlapEndPoint = Location + SquareSize;
        var num1 = IsRectangleOverlap(StartPoint, EndPoint, OverlapStartPoint, OverlapEndPoint) ? 1 : 0; 
        OverlapStartPoint = new FVector2D((double)FMath.Max(OverlapStartPoint.X, StartPoint.X), (double)FMath.Max(OverlapStartPoint.Y, StartPoint.Y));
        OverlapEndPoint = new FVector2D((double)FMath.Min(OverlapEndPoint.X, EndPoint.X), (double)FMath.Min(OverlapEndPoint.Y, EndPoint.Y));
        var num2 = (double)FMath.Max(OverlapStartPoint.X, StartPoint.X);
        var num3 = FMath.Min(OverlapEndPoint.X, EndPoint.X);
        var x = (double)GridInfo.CenterLocation.X;
        var num4 = (float)(num2 - x);
        var num5 = num4 % gridSize;
        var num6 = 4 + (int)((double)num4 / (double)gridSize) + ((double)num4 < 0.0 ? -1 : 1) * ((double)FMath.Abs(num5) > (double)gridSize / 2.0 ? 1 : 0);
        var num7 = num3 - GridInfo.CenterLocation.X;
        var num8 = num7 % gridSize;
        var num9 = 4 + (int)((double)num7 / (double)gridSize) + ((double)num7 < 0.0 ? -1 : 1) * ((double)FMath.Abs(num8) > (double)gridSize / 2.0 ? 1 : 0);
        var num10 = (double)FMath.Max(OverlapStartPoint.Y, StartPoint.Y);
        var num11 = FMath.Min(OverlapEndPoint.Y, EndPoint.Y);
        var y = (double)GridInfo.CenterLocation.Y;
        var num12 = (float)(num10 - y);
        var num13 = num12 % gridSize;
        var num14 = 4 + (int)((double)num12 / (double)gridSize) + ((double)num12 < 0.0 ? -1 : 1) * ((double)FMath.Abs(num13) > (double)gridSize / 2.0 ? 1 : 0);
        var num15 = num11 - GridInfo.CenterLocation.Y;
        var num16 = num15 % gridSize;
        var num17 = 4 + (int)((double)num15 / (double)gridSize) + ((double)num15 < 0.0 ? -1 : 1) * ((double)FMath.Abs(num16) > (double)gridSize / 2.0 ? 1 : 0);
        OutIndexList.Add(num6);
        OutIndexList.Add(num9);
        OutIndexList.Add(num14);
        OutIndexList.Add(num17);
        return num1 != 0;
    }
}
