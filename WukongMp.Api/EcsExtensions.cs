using System.Collections.Generic;
using System.Linq;
using b1;
using HarmonyLib;

namespace WukongMp.Api;

internal static class EcsExtensions
{
    public static T? GetComponent<T>(this BGUActorBaseCS actor) where T: UActorCompBaseCS
    {
        return Traverse
            .Create(actor.ActorCompContainerCS)
            .Field<List<UActorCompBaseCS>>("CompCSs").Value
            .FirstOrDefault(x => x is T) as T;
    }
}