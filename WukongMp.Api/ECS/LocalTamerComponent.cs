using System.Runtime.InteropServices;
using b1;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS;

[StructLayout(LayoutKind.Sequential)]
public struct LocalTamerComponent
{
    public bool IsSynced;
    public bool IsMonsterSpawned;
    public bool RunImmobilizePatches;
    public MontageState MontageState;

    private BUTamerActor? _tamer;

    public BUTamerActor? Tamer
    {
        get
        {
            if (_tamer.IsNullOrDestroyed())
            {
                return null;
            }

            return _tamer;
        }
        set => _tamer = value;
    }

    public BGUCharacterCS? Pawn
    {
        get
        {
            if (!IsMonsterSpawned)
            {
                return null;
            }

            if (_tamer == null || _tamer.IsNullOrDestroyed())
            {
                Logging.LogWarning("Tamer is null or destroyed in getPawn");
                return null;
            }

            if (_tamer.GetMonster().IsNullOrDestroyed())
            {
                Logging.LogWarning("Monster is null or destroyed in getPawn");
                return null;
            }

            return _tamer.GetMonster();
        }
    }

    public bool IsTamerValid => !Tamer.IsNullOrDestroyed();
}