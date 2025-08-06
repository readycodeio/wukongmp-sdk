using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using UnrealEngine.Engine;

namespace WukongMp.Api.ECS.Components;

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
                Logging.LogTrace("Marker actor is destroyed");
                return null;
            }

            return field;
        }
        set;
    }
}