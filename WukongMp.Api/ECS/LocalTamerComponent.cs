using System.Runtime.InteropServices;
using b1;
using Friflo.Engine.ECS;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS;

[StructLayout(LayoutKind.Sequential)]
public record struct LocalTamerComponent : IIndexedComponent<BUTamerActor?>
{
    public bool IsSynced;
    public bool IsMonsterSpawned;
    public bool RunImmobilizePatches;
    public MontageState MontageState;

    public LocalTamerComponent(BUTamerActor tamer)
    {
        Tamer = tamer;
    }

    public BUTamerActor? Tamer
    {
        get => field.IsNullOrDestroyed() ? null : field;
        set;
    }

    public BGUCharacterCS? Pawn
    {
        get
        {
            if (!IsMonsterSpawned)
            {
                return null;
            }

            var tamer = Tamer;
            if (tamer == null)
            {
                Logging.LogWarning("Tamer is null or destroyed in getPawn");
                return null;
            }

            if (tamer.GetMonster().IsNullOrDestroyed())
            {
                Logging.LogWarning("Monster is null or destroyed in getPawn");
                return null;
            }

            return tamer.GetMonster();
        }
    }

    public bool IsTamerValid => !Tamer.IsNullOrDestroyed();

    public BUTamerActor? GetIndexedValue()
    {
        return Tamer;
    }
}