using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using UnrealEngine.Engine;

namespace WukongMp.Api.ECS;

[StructLayout(LayoutKind.Sequential)]
public struct MarkerComponent : IComponent
{
    public bool DestroyQueued;
    private AActor? _markerActor;

    public AActor? MarkerActor
    {
        get
        {
            if (_markerActor != null && _markerActor.IsNullOrDestroyed())
            {
                Logging.LogTrace("Marker actor is destroyed");
                return null;
            }

            return _markerActor;
        }
        set => _markerActor = value;
    }
}