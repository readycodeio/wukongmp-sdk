using System;
using System.Collections.Generic;
using System.Reflection;

namespace WukongMp.Api.Resources;

/// <summary>
/// Lookup by resource name, for the few places that only learn which string they need at runtime,
/// such as a localized chat message whose key arrived over the wire.
/// </summary>
public static partial class BuiltinTexts
{
    private static readonly Lazy<Dictionary<string, PropertyInfo>> ByName = new(BuildIndex);

    /// Null when no resource of that name exists.
    public static string? GetByName(string name)
        => ByName.Value.TryGetValue(name, out var property) ? (string?)property.GetValue(null) : null;

    private static Dictionary<string, PropertyInfo> BuildIndex()
    {
        var index = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);

        foreach (var property in typeof(BuiltinTexts).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (property.PropertyType == typeof(string) && property.GetIndexParameters().Length == 0)
                index[property.Name] = property;
        }

        return index;
    }
}
