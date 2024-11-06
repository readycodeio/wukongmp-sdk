using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WukongCSharpMod
{
    public static class AssemblySaver
    {
        [DllImport("psapi.dll", SetLastError = true)]
        static extern bool GetModuleInformation(
            IntPtr hProcess,
            IntPtr hModule,
            out ModuleInfo lpModInfo,
            uint cb
        );

        [StructLayout(LayoutKind.Sequential)]
        public struct ModuleInfo
        {
            public IntPtr lpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }

        public static void SaveAssemblyToDisk(Assembly assembly, string filePath)
        {
            // Get the main module of the assembly
            var module = assembly.ManifestModule;

            // Get the module handle
            var hModule = Marshal.GetHINSTANCE(module);

            // Get the current process handle
            var process = Process.GetCurrentProcess();
            var hProcess = process.Handle;

            // Retrieve module information
            if (!GetModuleInformation(hProcess, hModule, out ModuleInfo moduleInfo, (uint)Marshal.SizeOf(typeof(ModuleInfo))))
            {
                throw new InvalidOperationException("Could not retrieve module information.");
            }

            // Create a byte array to hold the assembly bytes
            byte[] assemblyBytes = new byte[moduleInfo.SizeOfImage];

            // Copy the assembly bytes from memory
            Marshal.Copy(moduleInfo.lpBaseOfDll, assemblyBytes, 0, (int)moduleInfo.SizeOfImage);

            // Write the bytes to the specified file
            File.WriteAllBytes(filePath, assemblyBytes);
        }
    }
}
