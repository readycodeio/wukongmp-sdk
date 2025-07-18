using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using UnrealEngine.Engine;
using WukongMp.Api.Old;

namespace WukongMp.Api.ECS;

[StructLayout(LayoutKind.Sequential)]
public struct MarkerComponent : IComponent
{
    public bool DestroyQueued;

    public AActor? MarkerActor
    {
        get
        {
            if (field != null && field.IsNullOrDestroyed())
            {
                return null;
            }

            return field;
        }
        set;
    }
}