using System;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Wukong.Common.ECS.Components;

namespace WukongMp.Sdk.Api.Implementation;

/// <summary>
/// Provides references for core entity archetypes in WukongMP.
/// </summary>
/// <remarks>
/// Ids are positional: they are handed out in the order archetypes are registered, and the client and the relay
/// server have to agree. Ids 2 and 3 belong to the cell and world archetypes, which Wukong does not use but still
/// registers on both sides so that everything after them lines up. Do not close the gap.
/// </remarks>
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
    public ArchetypeId TamerArchetype => new ArchetypeId(4);

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
    public ArchetypeId MainCharacterArchetype => new ArchetypeId(5);

    /// <summary>
    /// Global PvP state entity archetype. Used by the PvP mod.
    /// Components:<br/>
    /// * <see cref="PvpStateComponent"/>
    /// </summary>
    [Obsolete("Will be moved to the PvP mod in future refactoring.")]
    public ArchetypeId PvpStateArchetype => new ArchetypeId(6);
}