using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Mapping.Api;

namespace WukongMp.Api.Mapping;

public static class DataMappingExtensions
{
    public static void SyncToGame<TComponent, TValue>(this BoundField<TComponent, TValue> field, in TComponent component)
        where TComponent : IComponent
    {
        var value = field.Get(component);
        DI.Instance.FieldMappingRegistry.Get(field).SyncToGame(value);
    }

    public static TValue LoadFromGame<TComponent, TValue>(this BoundField<TComponent, TValue> field, ref TComponent component)
        where TComponent : IComponent
    {
        return DI.Instance.FieldMappingRegistry.Get(field).LoadFromGame(ref component);
    }

    public static void SyncToGame<TComponent, TValue, TContext>(this BoundField<TComponent, TValue, TContext> field, TContext context, in TComponent component)
        where TComponent : IComponent
    {
        var value = field.Get(component);
        DI.Instance.FieldMappingRegistry.Get(field).SyncToGame(context, value);
    }

    public static TValue LoadFromGame<TComponent, TValue, TContext>(this BoundField<TComponent, TValue, TContext> field, ref TComponent component, TContext context)
        where TComponent : IComponent
    {
        return DI.Instance.FieldMappingRegistry.Get(field).LoadFromGame(ref component, context);
    }
}