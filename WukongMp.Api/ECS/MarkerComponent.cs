using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;
using UnrealEngine.Engine;

namespace WukongMp.Api.ECS;

[StructLayout(LayoutKind.Sequential)]
public struct MarkerComponent : IComponent
{
    public bool DestroyQueued;

    [Ignore]
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