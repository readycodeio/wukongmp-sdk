using System.Reflection;
using b1;
using BtlB1;
using BtlShare;
using HarmonyLib;
using PreludeLib.Attributes;
using ReadyM.Api.Multiplayer.Mapping.Tags;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;

namespace WukongMp.Api.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchComplexSkillDoInteractAction
{
    [HarmonyTargetMethodHint("b1.BUIAComplexSkill", "DoInteractAction")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUIAComplexSkill:DoInteractAction");
    }

    public static void Prefix(AActor InteractiveActor, FUStInteractionMappingDesc Action)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (Action.ParamsInt.Count > 1 && InteractiveActor is BGUCharacterCS character)
        {
            if (!DI.Instance.MappingPolicyDir.IsMonsterTamerMapped(character, out var entity))
            {
                Logging.LogWarning("Failed to find entity for character {Name} when processing skillinteract.", character.GetName());
                return;
            }

            Logging.LogDebug("Sending skill interact for {Name} with ID {Id}.", character.GetName(), entity.Value.Entity.GetNetId());
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(new TamerSkillInteractEvent(entity.Value, Action.ParamsInt[1]), default(EmptyContext));
        }
    }
}

[HarmonyPatch(typeof(BGW_EffectTemplateList), nameof(BGW_EffectTemplateList.GetInteractTypeTemplate))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchGetInteractTypeTemplate
{
    public static bool Prefix(EInteractType InteractType, ref BUInteractTypeTemplate? __result)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        Logging.LogDebug("GetInteractTypeTemplate called for {Type}", InteractType);
        if (DI.Instance.GameplayConfiguration.IsInteractionAllowed(InteractType))
        {
            return true;
        }
        else
        {
            __result = null;
            return false;
        }
    }
}

[HarmonyPatch(typeof(BUS_InteractCompImpl), "TickPlayerInteractive")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchInterActivePreCheckFocus
{
    public static void Postfix(BUC_InteractData ___InteractData)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (!DI.Instance.GameplayConfiguration.IsInteractionAllowed(___InteractData.InteractiveUnitCommDesc.InteractType))
        {
            ___InteractData.InteractConstraint = EInteractConstraint.NpcHide;
            ___InteractData.InteractUIState = EInteractUIState.Invisiable;
        }
    }
}