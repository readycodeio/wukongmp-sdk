using HarmonyLib;
using ReadyM.Api;

namespace WukongMp.Api;

public class HarmonyPatcherBase(string harmonyId) : PatcherBase
{
    protected readonly Harmony Harmony = new(harmonyId);
}