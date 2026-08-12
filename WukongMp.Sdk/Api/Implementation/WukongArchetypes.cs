using System;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Sdk.Api.Implementation;

/// <summary>
/// Provides references for core entity archetypes in WukongMP.
/// </summary>
public class WukongArchetypes
{
    /// Global player entity archetype.
    /// Components:<br/>
    /// * <see cref="AreaScopeComponent"/><br/>
    /// * <see cref="RoomComponent"/><br/>
    /// * <see cref="MovieComponent"/>
    public ArchetypeId AreaArchetype => new ArchetypeId(0);

    /// <summary>
    /// Global player entity archetype.
    /// Components:<br/>
    /// * <see cref="PlayerScopeComponent"/><br/>
    /// * <see cref="PlayerComponent"/>
    /// </summary>
    public ArchetypeId GlobalPlayerArchetype => new ArchetypeId(1);

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
    public ArchetypeId TamerArchetype => new ArchetypeId(3);

    /// <summary>
    /// Area-scoped main character entity archetype.
    /// Components:<br/>
    /// * <see cref="MainCharacterComponent"/><br/>
    /// * <see cref="TransformComponent"/><br/>
    /// * <see cref="HpComponent"/><br/>
    /// * <see cref="NicknameComponent"/><br/>
    /// * <see cref="TeamComponent"/><br/>
    /// * <see cref="PvPComponent"/><br/>
    /// </summary>
    public ArchetypeId MainCharacterArchetype => new ArchetypeId(4);

    /// <summary>
    /// Global PvP state entity archetype. Used by the PvP mod.
    /// Components:<br/>
    /// * <see cref="PvpStateComponent"/>
    /// </summary>
    [Obsolete("Will be moved to the PvP mod in future refactoring.")]
    public ArchetypeId PvpStateArchetype => new ArchetypeId(5);
}