using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using WukongMp.Api.ECS.Values;

namespace WukongMp.Api.ECS.Components;

[StructLayout(LayoutKind.Sequential)]
public struct LocalTamerComponent : IComponent
{
    public bool IsTamerSynced;
    public bool IsMonsterActive;
    
    public MontageStateData MontageState;

    // Has the game spawned monster for this tamer (refers to the local game state)
    public bool IsLocallySpawned;
    public bool HasPendingUnload;
    public bool IsCheckedForDead;
}
