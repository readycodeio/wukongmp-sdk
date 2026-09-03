using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Sdk.Serverside;

/// <summary>
/// Provides references for core entity archetypes in WukongMP.
/// </summary>
/// <remarks>
/// Ids are positional: they are handed out in the order archetypes are registered, and the client and the relay
/// server have to agree.
/// </remarks>
public static class WukongArchetypes
{
    /// Global player entity archetype.
    /// Components:<br/>
    /// * <see cref="AreaScopeComponent"/><br/>
    /// * <see cref="MovieComponent"/>
    public static ArchetypeId AreaArchetype => new(0);

    /// <summary>
    /// Global player entity archetype.
    /// Components:<br/>
    /// * <see cref="PlayerScopeComponent"/><br/>
    /// * <see cref="PlayerComponent"/>
    /// </summary>
    public static ArchetypeId GlobalPlayerArchetype => new(1);
    
    /// Global singleton archetype.
    public static ArchetypeId WorldArchetype => new(3);

    /// <summary>
    /// Area-scoped tamer entity archetype.
    /// Components:<br/>
    /// * <see cref="TamerComponent"/><br/>
    /// * <see cref="TransformComponent"/><br/>
    /// * <see cref="HpComponent"/><br/>
    /// * <see cref="NicknameComponent"/><br/>
    /// * <see cref="TeamComponent"/><br/>
    /// * <see cref="AnimationComponent"/><br/>
    /// * <see cref="MonsterAnimationComponent"/>
    /// </summary>
    public static ArchetypeId TamerArchetype => new(4);

    /// <summary>
    /// Area-scoped main character entity archetype.
    /// Components:<br/>
    /// * <see cref="MainCharacterComponent"/><br/>
    /// * <see cref="TransformComponent"/><br/>
    /// * <see cref="HpComponent"/><br/>
    /// * <see cref="NicknameComponent"/><br/>
    /// * <see cref="TeamComponent"/><br/>
    /// </summary>
    public static ArchetypeId MainCharacterArchetype => new(5);
}