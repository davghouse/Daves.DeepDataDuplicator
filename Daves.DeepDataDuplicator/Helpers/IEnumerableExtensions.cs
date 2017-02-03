using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Daves.DeepDataDuplicator.Helpers
{
    internal static class IEnumerableExtensions
    {
        internal static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source)
            => new ReadOnlyCollection<T>(source.ToList());

        internal static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source)
            {
                action(element);
            }
        }

        internal static IEnumerable<TSource> Prepend<TSource>(this IEnumerable<TSource> source, TSource element)
        {
            yield return element;

            foreach (var item in source)
                yield return item;
        }

        internal static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource element)
        {
            foreach (var item in source)
                yield return item;

            yield return element;
        }
    }
}
