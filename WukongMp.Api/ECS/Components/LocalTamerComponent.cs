using System.Runtime.InteropServices;
using b1;
using Friflo.Engine.ECS;
using WukongMp.Api.ECS.Values;

namespace WukongMp.Api.ECS.Components;

[StructLayout(LayoutKind.Sequential)]
public struct LocalTamerComponent(BUTamerActor tamer) : IComponent
{
    public bool IsTamerSynced;
    public bool IsMonsterSynced;
    public bool RunImmobilizePatches;
    public MontageState MontageState;
    public bool IsLocallySpawned;

    private BUTamerActor? _tamer = tamer;
    
    public BUTamerActor? Tamer
    {
        get => _tamer.IsNullOrDestroyed() ? null : _tamer;
        set => _tamer = value;
    }

    public BGUCharacterCS? Pawn
    {
        get
        {
            if (!IsMonsterSynced)
            {
                return null;
            }

            var tamer = Tamer;
            if (tamer == null)
            {
                Logging.LogWarning("Tamer is null or destroyed in getPawn");
                return null;
            }

            var monster = tamer.GetMonster();
            return monster.IsNullOrDestroyed() ? null : monster;
        }
    }

    public bool IsTamerValid => !Tamer.IsNullOrDestroyed();
}