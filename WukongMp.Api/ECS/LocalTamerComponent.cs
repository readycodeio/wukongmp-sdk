using System.Runtime.InteropServices;
using b1;
using Friflo.Engine.ECS;
using WukongMp.Api.Old;
using WukongMp.Api.Old.State;

namespace WukongMp.Api.ECS;

[StructLayout(LayoutKind.Sequential)]
public struct LocalTamerComponent(BUTamerActor tamer) : IComponent
{
    public bool IsTamerSynced;
    public bool IsMonsterSynced;
    public bool RunImmobilizePatches;
    public MontageState MontageState;
    public int SpawnedCounter;
    public bool IsLocallySpawned;

    public BUTamerActor? Tamer
    {
        get => field.IsNullOrDestroyed() ? null : field;
        set;
    } = tamer;

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