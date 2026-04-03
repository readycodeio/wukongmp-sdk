using System.Collections.Generic;
using System.Linq;
using b1;
using HarmonyLib;

namespace WukongMp.Api;

public static class EcsExtensions
{
    /// <summary>
    /// Get a **game ECS** component of type <typeparamref name="T"/> from the given actor.
    /// </summary>
    /// <param name="actor">The actor to get the component from.</param>
    /// <typeparam name="T">The type of the component to get. Must be a subclass of <see cref="UActorCompBaseCS"/>.</typeparam>
    /// <returns>The component of type <typeparamref name="T"/> if found; otherwise, <c>null</c>.</returns>
    public static T? GetComponent<T>(this BGUActorBaseCS actor) where T : UActorCompBaseCS
    {
        return Traverse
            .Create(actor.ActorCompContainerCS)
            .Field<List<UActorCompBaseCS>>("CompCSs").Value
            .FirstOrDefault(x => x is T) as T;
    }
}