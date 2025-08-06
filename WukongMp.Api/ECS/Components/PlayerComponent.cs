using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Idents;

namespace WukongMp.Api.ECS.Components;

public struct PlayerComponent() : IComponent
{
    public PlayerId PlayerId { get; set; }
    
    // NOTE: This defines the player name used in chat etc.
    public string NickName { get; set; } = "";
    
    public bool IsReadyForPvP { get; set; }
    public bool IsSpectator { get; set; }
    
    // NOTE: This is the players' Team ID, used in PvP, possibly in the future in creative mode
    // This is separate separated out from the TeamID on the main character which describes directly the team of the
    // underlying game actor.
    public int TeamId { get; set; }
}