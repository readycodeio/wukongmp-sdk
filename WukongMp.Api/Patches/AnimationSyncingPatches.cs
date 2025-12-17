using b1;
using HarmonyLib;
using UnrealEngine.Engine;
using WukongMp.Api.Configuration;

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
                return DI.Instance.ClientOwnership.OwnsEntity(entity.Value.Entity);
            }
        }

        return true;
    }
}
