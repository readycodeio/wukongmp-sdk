using System;
using System.Runtime.InteropServices;
using b1;
using WukongApi.State;

namespace WukongApi.ECS;

[StructLayout(LayoutKind.Sequential)]
public struct TamerComponent
{
    public bool IsSynced;
    public bool RunImmobilizePatches;
    public MontageState MontageState;
    
    public string UnitName;
    public string Guid;
    
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
            if (_tamer == null || _tamer.IsNullOrDestroyed() || _tamer.GetMonster().IsNullOrDestroyed())
            {
                Logging.LogWarning("Tamer or monster is null or destroyed");
                return null;
            }

            return _tamer.GetMonster();
        }
        set => throw new NotSupportedException("Set monster pawn");
    }
    
    public bool IsTamerValid => !Tamer.IsNullOrDestroyed();
}