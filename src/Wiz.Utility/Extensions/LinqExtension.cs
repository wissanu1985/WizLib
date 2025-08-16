using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wiz.Utility.Extensions
{
    /// <summary>
    /// High-performance LINQ-related helpers focused on minimizing allocations and extra passes.
    /// </summary>
    public static class LinqExtension
    {
        /// <summary>
        /// Asynchronously tries to get the first element without throwing when the source is empty.
        /// Returns a tuple (success, value).
        /// </summary>
        public static async ValueTask<(bool Success, T Value)> TryFirstAsync<T>(
            this IAsyncEnumerable<T> source,
            CancellationToken cancellationToken = default)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            await using var e = source.GetAsyncEnumerator(cancellationToken);
            if (await e.MoveNextAsync().ConfigureAwait(false))
            {
                return (true, e.Current!);
            }

            return (false, default!);
        }

        /// <summary>
        /// Asynchronously tries to get the single element. Returns (false, default) if none or more than one.
        /// </summary>
        public static async ValueTask<(bool Success, T Value)> TrySingleAsync<T>(
            this IAsyncEnumerable<T> source,
            CancellationToken cancellationToken = default)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            await using var e = source.GetAsyncEnumerator(cancellationToken);
            if (!await e.MoveNextAsync().ConfigureAwait(false))
            {
                return (false, default!);
            }
            var first = e.Current;
            if (await e.MoveNextAsync().ConfigureAwait(false))
            {
                return (false, default!); // more than one
            }
            return (true, first!);
        }

        /// <summary>
        /// Fast-path overload for <see cref="IReadOnlyList{T}"/>: O(1) index access.
        /// </summary>
        public static bool TryFirst<T>(this IReadOnlyList<T> source, out T value)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (source.Count > 0)
            {
                value = source[0]!;
                return true;
            }
            value = default!;
            return false;
        }

        /// <summary>
        /// Tries to get the first element without throwing when the source is empty.
        /// Avoids materialization and exceptions. Returns true if a value exists.
        /// </summary>
        public static bool TryFirst<T>(this IEnumerable<T> source, out T value)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            // Fast paths
            if (source is IList<T> list)
            {
                if (list.Count > 0)
                {
                    value = list[0]!;
                    return true;
                }
                value = default!;
                return false;
            }
            if (source is IReadOnlyList<T> roList)
            {
                if (roList.Count > 0)
                {
                    value = roList[0]!;
                    return true;
                }
                value = default!;
                return false;
            }

            using var e = source.GetEnumerator();
            if (e.MoveNext())
            {
                value = e.Current!;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Fast-path overload for <see cref="IReadOnlyList{T}"/>.
        /// </summary>
        public static bool TrySingle<T>(this IReadOnlyList<T> source, out T value)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (source.Count == 1)
            {
                value = source[0]!;
                return true;
            }
            value = default!;
            return false;
        }

        /// <summary>
        /// Tries to get the single element without throwing. Returns false if none or more than one.
        /// </summary>
        public static bool TrySingle<T>(this IEnumerable<T> source, out T value)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            // Fast path for lists
            if (source is IList<T> list)
            {
                if (list.Count == 1)
                {
                    value = list[0]!;
                    return true;
                }

                value = default!;
                return false;
            }
            if (source is IReadOnlyList<T> roList)
            {
                if (roList.Count == 1)
                {
                    value = roList[0]!;
                    return true;
                }
                value = default!;
                return false;
            }

            using var e = source.GetEnumerator();
            if (!e.MoveNext())
            {
                value = default!;
                return false;
            }
            var first = e.Current;
            if (e.MoveNext())
            {
                value = default!;
                return false; // more than one
            }
            value = first!;
            return true;
        }

        /// <summary>
        /// Materializes to an array with minimal allocations.
        /// If the source has a known count, allocates exactly once and fills directly.
        /// </summary>
        public static T[] ToArrayFast<T>(this IEnumerable<T> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            if (source is ICollection<T> coll)
            {
                var count = coll.Count;
                if (count == 0) return Array.Empty<T>();
                var arr = new T[count];
                var i = 0;
                foreach (var item in coll)
                {
                    arr[i++] = item!;
                }
                return arr;
            }
            else if (source is IReadOnlyCollection<T> ro)
            {
                var count = ro.Count;
                if (count == 0) return Array.Empty<T>();
                var arr = new T[count];
                var i = 0;
                foreach (var item in ro)
                {
                    arr[i++] = item!;
                }
                return arr;
            }

            // Fallback to framework-optimized path
            return Enumerable.ToArray(source);
        }

        /// <summary>
        /// Materializes to a List with preallocated capacity when possible.
        /// </summary>
        public static List<T> ToListFast<T>(this IEnumerable<T> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            if (source is ICollection<T> coll)
            {
                var count = coll.Count;
                if (count == 0) return new List<T>(0);
                var list = new List<T>(count);
                foreach (var item in coll)
                {
                    list.Add(item);
                }
                return list;
            }
            else if (source is IReadOnlyCollection<T> ro)
            {
                var count = ro.Count;
                if (count == 0) return new List<T>(0);
                var list = new List<T>(count);
                foreach (var item in ro)
                {
                    list.Add(item);
                }
                return list;
            }

            // Fallback
            return Enumerable.ToList(source);
        }

        /// <summary>
        /// Projects and materializes to a List with preallocated capacity when the count is known.
        /// Performs a single pass.
        /// </summary>
        public static List<TResult> SelectToList<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (selector is null) throw new ArgumentNullException(nameof(selector));

            if (source is ICollection<T> coll)
            {
                var count = coll.Count;
                if (count == 0) return new List<TResult>(0);
                var list = new List<TResult>(count);
                foreach (var item in coll)
                {
                    list.Add(selector(item));
                }
                return list;
            }
            else if (source is IReadOnlyCollection<T> ro)
            {
                var count = ro.Count;
                if (count == 0) return new List<TResult>(0);
                var list = new List<TResult>(count);
                foreach (var item in ro)
                {
                    list.Add(selector(item));
                }
                return list;
            }

            // Unknown count; allow List to grow
            var result = new List<TResult>();
            foreach (var item in source)
            {
                result.Add(selector(item));
            }
            return result;
        }

        /// <summary>
        /// Filters and projects in a single pass, materializing to a List. Uses capacity hint when available.
        /// </summary>
        public static List<TResult> WhereSelectToList<T, TResult>(
            this IEnumerable<T> source,
            Func<T, bool> predicate,
            Func<T, TResult> selector)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));
            if (selector is null) throw new ArgumentNullException(nameof(selector));

            var capacity = (source as ICollection<T>)?.Count
                           ?? (source as IReadOnlyCollection<T>)?.Count
                           ?? 0; // upper bound
            var list = capacity > 0 ? new List<TResult>(capacity) : new List<TResult>();
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    list.Add(selector(item));
                }
            }
            // Trim if we over-allocated significantly; avoid TrimExcess on small lists to keep it cheap
            if (capacity > 0 && list.Count < (capacity >> 1))
            {
                list.TrimExcess();
            }
            return list;
        }

        /// <summary>
        /// Executes the action for each element in a single pass. Avoids allocations from higher-level LINQ operators.
        /// </summary>
        public static void ForEachFast<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (action is null) throw new ArgumentNullException(nameof(action));

            foreach (var item in source)
            {
                action(item);
            }
        }

        /// <summary>
        /// Executes the action with index for each element. Index is zero-based.
        /// </summary>
        public static void ForEachFast<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (action is null) throw new ArgumentNullException(nameof(action));

            var index = 0;
            foreach (var item in source)
            {
                action(item, index++);
            }
        }

        

        /// <summary>
        /// Asynchronously projects all items and materializes to a List. Uses capacity hint when available.
        /// </summary>
        public static async ValueTask<List<TResult>> SelectToListAsync<T, TResult>(
            this IAsyncEnumerable<T> source,
            Func<T, TResult> selector,
            CancellationToken cancellationToken = default)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (selector is null) throw new ArgumentNullException(nameof(selector));

            // Capacity hint if the async source also implements collection interfaces
            var capacity = (source as ICollection<T>)?.Count
                           ?? (source as IReadOnlyCollection<T>)?.Count
                           ?? 0;
            var list = capacity > 0 ? new List<TResult>(capacity) : new List<TResult>();

            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                list.Add(selector(item));
            }
            return list;
        }

        /// <summary>
        /// Asynchronously filters and projects in a single pass, materializing to a List. Uses capacity hint when available.
        /// </summary>
        public static async ValueTask<List<TResult>> WhereSelectToListAsync<T, TResult>(
            this IAsyncEnumerable<T> source,
            Func<T, bool> predicate,
            Func<T, TResult> selector,
            CancellationToken cancellationToken = default)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (predicate is null) throw new ArgumentNullException(nameof(predicate));
            if (selector is null) throw new ArgumentNullException(nameof(selector));

            var capacity = (source as ICollection<T>)?.Count
                           ?? (source as IReadOnlyCollection<T>)?.Count
                           ?? 0; // upper bound
            var list = capacity > 0 ? new List<TResult>(capacity) : new List<TResult>();

            await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (predicate(item))
                {
                    list.Add(selector(item));
                }
            }

            if (capacity > 0 && list.Count < (capacity >> 1))
            {
                list.TrimExcess();
            }
            return list;
        }
    }
}