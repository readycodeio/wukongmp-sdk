using b1;
using HarmonyLib;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BGS_AnimationSyncSystem), "OnBeginAnimationSyncPreCheck")]
[HarmonyPatchCategory(Constants.ConnectedPatches)]
public class PatchOnBeginAnimationSyncPreCheck
{
    // Disable animation syncing attacks for monsters not owned by the local player
    public static bool Prefix(AActor Host)
    {
        var owner = Host as BGU_CharacterAI;

        if (owner != null)
        {
            var entity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (entity.HasValue)
            {
                var target = TargetingUtils.GetTarget(owner);
                if (target != null)
                {
                    // another player is being attacked by a monster, only allow for owned monsters
                    return DI.Instance.ClientOwnership.OwnsEntity(entity.Value.Entity);
                }
            }
        }

        return true;
    }
}