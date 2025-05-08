using System.Runtime.InteropServices;

namespace WukongApi.ECS;

[StructLayout(LayoutKind.Sequential)]
public struct LocalDeathComponent
{
    public bool killed;
}