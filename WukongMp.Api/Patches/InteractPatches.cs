using System.Reflection;
using b1;
using BtlB1;
using BtlShare;
using HarmonyLib;
using PreludeLib.Attributes;
using ReadyM.Relay.Common.Mapping;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;

namespace WukongMp.Api.Patches;

[HarmonyPatch]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public static class PatchComplexSkillDoInteractAction
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

        if (Action.ParamsInt.Count > 1 && InteractiveActor is BGUCharacterCS)
        {
            // TODO: Before refactoring this only checked Tamers
            if (!DI.Instance.MappedEntity.IsMapped(InteractiveActor, out var entity))
                return;

            Logging.LogDebug("Sending skill interact for {Name} with ID {Id}.", InteractiveActor.GetName(), entity.Value.GetNetId());
            DI.Instance.MappedEvent.NotifyEcsIfApplicable(new TamerSkillInteractEvent(entity.Value, Action.ParamsInt[1]), default(EmptyContext));
        }
    }
}

[HarmonyPatch(typeof(BGW_EffectTemplateList), nameof(BGW_EffectTemplateList.GetInteractTypeTemplate))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchGetInteractTypeTemplate
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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchInterActivePreCheckFocus
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