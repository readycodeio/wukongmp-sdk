using System;
using System.Collections.Generic;

namespace WukongMp.Api.Common;

public static class ListExtensions
{
    public static void Resize<T>(this List<T?> list, int size, T? element = default)
    {
        var count = list.Count;
        if (size == count)
            return;
        
        if (size < count)
        {
            list.RemoveRange(size, count - size);
            return;
        }
        
        if (size > list.Capacity)
            list.Capacity = Math.Max(list.Capacity * 2, size);

        for (var i = count; i < size; i++)
        {
            list.Add(element!);
        }
    }
    
    public static void EnsureLength<T>(this List<T?> list, int size, T? element = default)
    {
        var count = list.Count;
        if (size <= count)
            return;

        if (size > list.Capacity)
            list.Capacity = Math.Max(list.Capacity * 2, size);
        
        for (var i = count; i < size; i++)
        {
            list.Add(element!);
        }
    }
}