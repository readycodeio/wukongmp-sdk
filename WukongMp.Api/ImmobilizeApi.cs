using Friflo.Engine.ECS;
using WukongMp.Api.ECS;
using WukongMp.Api.Old;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

// TODO: Eventually we'll expose APIs like this one (accepting Entities) that will wrap the WukongUtils classes
public static class ImmobilizeApi
{
    public static void CastImmobilize(Entity caster)
    {
        if (caster.TryGetComponent<LocalTamerComponent>(out var tamer) && tamer.IsTamerValid)
        {
            ImmobilizeUtils.CastImmobilize(tamer.Pawn!);
        }
        else
        {
            Logging.LogError("Entity {Entity} does not have a valid Pawn", caster);
        }
    }

    public static void TriggerImmobilize(Entity caster, Entity target, bool hasBuff)
    {
        if (caster.TryGetComponent<LocalTamerComponent>(out var tamer) && tamer.IsTamerValid)
        {
            if (target.TryGetComponent<LocalTamerComponent>(out var targetTamer) && targetTamer.IsTamerValid)
            {
                ImmobilizeUtils.TriggerImmobilize(tamer.Pawn, targetTamer.Pawn, hasBuff);
            }
            else
            {
                Logging.LogError("Target {Target} does not have a valid Pawn", target);
            }
        }
        else
        {
            Logging.LogError("Entity {Entity} does not have a valid Pawn", caster);
        }
    }

    public static void RelieveImmobilize(Entity target)
    {
        if (target.TryGetComponent<LocalTamerComponent>(out var tamer) && tamer.IsTamerValid)
        {
            ImmobilizeUtils.RelieveImmobilize(tamer.Pawn!);
        }
        else
        {
            Logging.LogError("Entity {Entity} does not have a valid Pawn", target);
        }
    }
}