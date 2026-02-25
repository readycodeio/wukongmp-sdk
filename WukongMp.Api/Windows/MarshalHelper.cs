using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WukongMp.Api.Windows;

public static class MarshalHelper
{
    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public static IntPtr GetHINSTANCE(Module module)
    {
        var hInst = module.ModuleHandle;
        if (hInst == ModuleHandle.EmptyHandle)
            throw new Win32Exception(Marshal.GetLastWin32Error());
        return GetModuleHandle(module.Name);
    }
}