using System;
using System.Collections.Generic;
using System.Linq;

namespace Daves.DankDataDuplicator.Helpers
{
    internal static class IEnumerableExtensions
    {
        internal static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source)
            => source.ToList().AsReadOnly();

        internal static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source)
                action(element);
        }
    }
}
