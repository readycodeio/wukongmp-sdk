using System.Diagnostics;

namespace WukongMp.Api.Compat;

public static class DebugHelper
{
    [Conditional("DEBUG")]
    public static void AssertNotNull<T>([NotNull] T? value)
        => Debug.Assert(value is not null);
    
    [Conditional("DEBUG")]
    public static void AssertNotNull<T>([NotNull] T? value, string message)
        => Debug.Assert(value is not null, message);
}