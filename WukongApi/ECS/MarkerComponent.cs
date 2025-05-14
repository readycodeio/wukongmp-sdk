using System.Runtime.InteropServices;
using UnrealEngine.Engine;

namespace WukongApi.ECS;

[StructLayout(LayoutKind.Sequential)]
public struct MarkerComponent
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