using b1;
using System;
using System.Collections.Generic;
using System.Text;
using UnrealEngine.Engine;
using WukongMp.Api.Old;

namespace WukongMp.Api.WukongUtils
{
    public static class TamerUtils
    {
        public static IEnumerable<BGUCharacterCS> GetMonsters()
        {
            var world = GameUtils.GetWorld();
            if (world == null)
                yield break;

            var actors = world.GetAllActorsOfClass<BUTamerActor>();
            foreach (var actor in actors)
            {
                Logging.LogDebug("Found actor: {ActorName}", actor.GetName());

                var monster = actor.GetMonster();
                if (monster != null)
                {
                    Logging.LogDebug("Actor is a monster");
                    yield return monster;
                }
            }
        }

        public static void DestroyAllTamers()
        {
            var allActorsOfClass = UGameplayStatics.GetAllActorsOfClass<BUTamerActor>(GameUtils.GetWorld());
            foreach (var actor in allActorsOfClass)
            {
                if (actor != null && actor.GetMonster() != null)
                {
                    BGU_UnrealWorldUtil.DestroyActor(actor.GetMonster());
                }

                BGU_UnrealWorldUtil.DestroyActor(actor);
            }
        }

        public static string UnifyUnitName(string unitName)
        {
            return unitName.ToLower().Replace("-", "").Replace("_", "");
        }
    }
}
