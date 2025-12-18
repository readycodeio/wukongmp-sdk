using System;
using b1;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
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
                    if (target == DI.Instance.PlayerState.LocalMainCharacter?.GetLocalState().Pawn)
                    {
                        // we are being attacked by a monster, allow it
                        return true;
                    }

                    // another player is being attacked by a monster, only allow for owned monsters
                    return DI.Instance.ClientOwnership.OwnsEntity(entity.Value.Entity);
                }
            }
        }

        return true;
    }
}
