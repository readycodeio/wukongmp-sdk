using System.Runtime.InteropServices;
using b1;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;
using WukongMp.Api.ECS.Values;

namespace WukongMp.Api.ECS.Components;

[StructLayout(LayoutKind.Sequential)]
public struct LocalTamerComponent(BUTamerActor tamer) : IComponent
{
    public bool IsTamerSynced;
    public bool IsMonsterActive;
    public bool RunImmobilizePatches;
    public MontageState MontageState;
    public bool IsLocallySpawned;
    public bool HasPendingUnload;
    public bool IsCheckedForDead;

    private BUTamerActor? _tamer = tamer;
    
    [Ignore]
    public BUTamerActor? Tamer
    {
        get => _tamer.IsNullOrDestroyed() ? null : _tamer;
        set => _tamer = value;
    }

    [Ignore]
    public BGUCharacterCS? Pawn
    {
        get
        {
            if (!IsMonsterActive)
            {
                return null;
            }

            var tamer = Tamer;
            if (tamer == null)
            {
                Logging.LogDebug("Tamer is null or destroyed in getPawn");
                return null;
            }

            var monster = tamer.GetMonster();
            return monster.IsNullOrDestroyed() ? null : monster;
        }
    }

    public bool IsTamerValid => !Tamer.IsNullOrDestroyed();
}