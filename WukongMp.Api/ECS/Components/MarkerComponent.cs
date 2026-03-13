using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;
using UnrealEngine.Engine;

namespace WukongMp.Api.ECS.Components;

[StructLayout(LayoutKind.Sequential)]
internal struct MarkerComponent : IComponent
{
    public bool DestroyQueued;

    [Ignore]
    public AActor? MarkerActor
    {
        readonly get
        {
            if (field != null && field.IsNullOrDestroyed() || DestroyQueued)
            {
                return null;
            }

            return field;
        }
        set
        {
            field = value;
            DestroyQueued = false;
        }
    }
}