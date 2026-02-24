using System.Reflection;
using b1;
using BtlB1;
using BtlShare;
using HarmonyLib;
using PreludeLib.Attributes;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
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

    public static void Prefix(int InteractiveActorID, AActor User, AActor InteractiveActor, FUStInteractionMappingDesc Action)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (Action.ParamsInt.Count > 1 && InteractiveActor is BGUCharacterCS)
        {
            var entity = DI.Instance.PawnState.GetEntityByTamerMonster(InteractiveActor);
            if (entity.HasValue)
            {
                var meta = entity.Value.GetMeta();
                Logging.LogDebug("Sending skill interact for {Name} with ID {Id}.", InteractiveActor.GetName(), meta.NetId);
                DI.Instance.MappedEvent.PropagateToEcs(new TamerSkillInteractEvent(entity.Value, Action.ParamsInt[1]));
            }
        }
    }
}

[HarmonyPatch(typeof(BGW_EffectTemplateList), nameof(BGW_EffectTemplateList.GetInteractTypeTemplate))]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchGetInteractTypeTemplate
{
    public static bool Prefix(EInteractType InteractType, BUInteractTypeTemplate? __result)
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
