using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using HarmonyLib;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

public class HttpPatches
{
    [HarmonyPatch(typeof(ServicePointManager))]
    [HarmonyPatchCategory(Constants.GlobalPatches)]
    public static class ServicePointManagerPatch
    {
        private static bool _connectionInit;

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("System.Net.ServicePointManager:FindServicePoint", [
                typeof(Uri),
                typeof(IWebProxy)
            ]);
        }

        public static bool Prefix(ServicePoint __result, Uri address, IWebProxy proxy)
        {
            if (!_connectionInit)
            {
                var f = typeof(ServicePointManager).GetField("manager", BindingFlags.Static | BindingFlags.NonPublic);
                Debug.Assert(f != null);
                if (f!.GetValue(null) == null)
                {
                    var connDataType = f.FieldType;
                    Debug.Assert(connDataType != null);
                    var connData = Activator.CreateInstance(connDataType, [null]);
                    f.SetValue(null, connData);
                }

                _connectionInit = true;
            }

            return true;
        }
    }
}