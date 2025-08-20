using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;
using UnrealEngine.Engine;

namespace WukongMp.Api.ECS.Components;

[StructLayout(LayoutKind.Sequential)]
public struct MarkerComponent : IComponent
{
    public bool DestroyQueued;

    private AActor? _markerActor;
    
    [Ignore]
    public AActor? MarkerActor
    {
        get
        {
            if (_markerActor != null && _markerActor.IsNullOrDestroyed())
            {
                return null;
            }

            return _markerActor;
        }
        set => _markerActor = value;
    }
}