using System;
using System.Runtime.InteropServices;
using b1;
using WukongApi.State;

namespace WukongApi.ECS;

[StructLayout(LayoutKind.Sequential)]
public struct TamerComponent
{
    public string UnitName;
    public string Guid;
    public bool IsSynced;
    
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