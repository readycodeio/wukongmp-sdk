using System;
using System.Collections.Generic;
using System.Threading;

namespace WukongMp.Api.Compat;

internal static class CompatExtensions
{
    public static bool TryDequeue<T>(this Queue<T> queue, out T item)
    {
        if (queue == null) 
            throw new ArgumentNullException(nameof(queue));

        if (queue.Count > 0)
        {
            item = queue.Dequeue();
            return true;
        }
        item = default!;
        return false;
    }
    
    private static readonly ThreadLocal<char[]> _splitBuffer = new(() => new char[1]);

    public static string[] Split(this string str, char separator, StringSplitOptions options = StringSplitOptions.None)
    {
        var buffer = _splitBuffer.Value;
        buffer[0] = separator;
        return str.Split(buffer, options);
    }
    
    public static V GetValueOrDefault<K, V>(this Dictionary<K, V> dict, K key)
    {
        if (dict == null) 
            throw new ArgumentNullException(nameof(dict));
        if (key == null) 
            throw new ArgumentNullException(nameof(key));

        return dict.TryGetValue(key, out var value)
            ? value
            : default(V)!;
    }

    public static V GetValueOrDefault<K, V>(this Dictionary<K, V> dict, K key, V defaultValue)
    {
        if (dict == null) 
            throw new ArgumentNullException(nameof(dict));
        if (key == null) 
            throw new ArgumentNullException(nameof(key));

        return dict.TryGetValue(key, out var value)
            ? value
            : defaultValue;
    }
}