using Hector.Core.Support.Collections;
using Hector.Core.Support.Collections.Comparers.Equality;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hector.Core.Support
{
    public static class CollectionsExtensionMethods
    {
        public static ReadOnlyCollection<T> AsReadOnly<T>(this IEnumerable<T> list)
        {
            if (list.IsNullOrEmptyList())
            {
                return null;
            }

            return
                list
                    .ToList()
                    .AsReadOnly();
        }

        public static bool IsNullOrEmptyList<T>(this IEnumerable<T> list)
        {
            return list.IsNull() || !list.Any();
        }

        public static IEnumerable<T> ToEmptyIfNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.IsNull() ? new List<T>() : enumerable;
        }

        public static IList<T> ToEmptyListIfNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.ToEmptyIfNull().ToList();
        }

        public static T[] ToEmptyArrayIfNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.ToEmptyIfNull().ToArray();
        }

        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            if (sequence.IsNullOrEmptyList())
            {
                return;
            }

            foreach (var item in sequence.ToList())
            {
                action(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T, int> action)
        {
            if (sequence.IsNullOrEmptyList())
            {
                return;
            }

            int index = 0;
            foreach (var item in sequence)
            {
                action(item, index);
                ++index;
            }
        }

        public static IList<Tuple<TResult>> ToTupleList<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            source.AssertNotNull("source");
            selector.AssertNotNull("selector");

            return
                source
                    .ToEmptyIfNull()
                    .Select(x => new Tuple<TResult>(selector(x)))
                    .ToList();
        }

        public static IList<Tuple<T1, T2>> ToTupleList<TSource, T1, T2>(this IEnumerable<TSource> source, Func<TSource, T1> selector1, Func<TSource, T2> selector2)
        {
            source.AssertNotNull("source");
            selector1.AssertNotNull("selector1");
            selector2.AssertNotNull("selector2");

            return
                source
                    .ToEmptyIfNull()
                    .Select(x => new Tuple<T1, T2>(selector1(x), selector2(x)))
                    .ToList();
        }

        public static IList<Tuple<T1, T2, T3>> ToTupleList<TSource, T1, T2, T3>(this IEnumerable<TSource> source, Func<TSource, T1> selector1, Func<TSource, T2> selector2, Func<TSource, T3> selector3)
        {
            source.AssertNotNull("source");
            selector1.AssertNotNull("selector1");
            selector2.AssertNotNull("selector2");
            selector3.AssertNotNull("selector3");

            return
                source
                    .ToEmptyIfNull()
                    .Select(x => new Tuple<T1, T2, T3>(selector1(x), selector2(x), selector3(x)))
                    .ToList();
        }

        public static TResult SelectFirst<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            source.AssertNotNull("source");
            selector.AssertNotNull("selector");

            return source.Select(selector).FirstOrDefault();
        }

        public static TResult SelectLast<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            source.AssertNotNull("source");
            selector.AssertNotNull("selector");

            return source.Select(selector).LastOrDefault();
        }

        public static bool IsIn<T>(this T item, params T[] list)
        {
            return IsIn(item, list.AsEnumerable());
        }

        public static bool IsIn<T>(this T item, Func<T, T, bool> eqFx, params T[] list)
        {
            return IsIn(item, list.AsEnumerable(), eqFx);
        }

        public static bool IsIn<T>(this T item, IEnumerable<T> list)
        {
            return IsIn(item, list, (x, y) => x.Equals(y));
        }

        public static bool IsIn<T>(this T item, IEnumerable<T> enumerablesList, Func<T, T, bool> eqFx)
        {
            item.AssertNotNull("item");
            eqFx.AssertNotNull("eqFx");

            if (enumerablesList.IsNullOrEmptyList())
            {
                return false;
            }

            return enumerablesList.Any(listItem => eqFx(item, listItem));
        }

        public static IList<T> AsList<T>(this T s, int size = 1)
        {
            return new List<T> { s };
        }

        public static T[] AsArray<T>(this T item, int size = 1)
        {
            T[] array = new T[size];
            for (int i = 0; i < size; ++i)
            {
                array[i] = item;
            }
            return array;
        }

        public static T[] AsArrayOrNull<T>(this T item, int size = 1)
        {
            return item.IsNull() ? null : item.AsArray(size);
        }

        public static IList<T> AsListOrNull<T>(this T item, int size = 1)
        {
            return item.IsNull() ? null : item.AsList(size);
        }

        public static IEnumerable<OuterLinqJoinResult<TOuter, TInner>> LeftJoin<TOuter, TInner, TKey>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector)
        {
            return LeftJoin(outer, inner, outerKeySelector, innerKeySelector, EqualityComparer<TKey>.Default);
        }

        public static IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<OuterLinqJoinResult<TOuter, TInner>, TResult> resultSelector)
        {
            return
                LeftJoin(outer, inner, outerKeySelector, innerKeySelector)
                    .Select(x => resultSelector(x));
        }

        public static IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<OuterLinqJoinResult<TOuter, TInner>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            return
                LeftJoin(outer, inner, outerKeySelector, innerKeySelector, comparer)
                    .Select(x => resultSelector(x));
        }

        public static IEnumerable<OuterLinqJoinResult<TOuter, TInner>> LeftJoin<TOuter, TInner, TKey>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, IEqualityComparer<TKey> comparer)
        {
            inner.AssertNotNull("outer");
            outer.AssertNotNull("outer");

            return
                outer
                    .GroupJoin
                    (
                        inner,
                        outerKeySelector,
                        innerKeySelector,
                        (x, y) =>
                            new
                            {
                                LeftPart = x,
                                RightPart = y
                            },
                        comparer
                    )
                    .SelectMany
                    (
                        x => x.RightPart.DefaultIfEmpty(),
                        (x, y) =>
                            new OuterLinqJoinResult<TOuter, TInner>
                            {
                                LeftPart = x.LeftPart,
                                RightPart = y
                            }
                    );
        }

        public static IList<T> Clone<T>(this IList<T> list) where T : ICloneable
        {
            return
                list
                    .ToList()
                    .ConvertAll(x => (T)x.Clone());
        }

        public static T SafeElementAt<T>(this IEnumerable<T> list, int index, T failureValue = default(T))
        {
            try
            {
                return list.ElementAt(index);
            }
            catch (ArgumentOutOfRangeException)
            {
                return failureValue;
            }
        }

        public static TValue SafeGetValue<TItem, TValue>(this IEnumerable<TItem> list, int index, Func<TItem, TValue> selector, TValue failureValue = default(TValue))
        {
            try
            {
                return selector(list.ElementAt(index));
            }
            catch (ArgumentOutOfRangeException)
            {
                return failureValue;
            }
        }

        public static IEnumerable<T> ToNullIfEmptyList<T>(this IEnumerable<T> list)
        {
            return list.IsNullOrEmptyList() ? null : list;
        }

        public static IEnumerable<List<T>> Split<T>(this List<T> list, int chunkSize)
        {
            if (chunkSize <= 0)
            {
                throw new FormatException("The size cannot be <= 0");
            }

            for (int i = 0; i < list.Count; i += chunkSize)
            {
                yield return list.GetRange(i, Math.Min(chunkSize, list.Count - i));
            }
        }

        public static IEnumerable<T[]> Split<T>(this T[] list, int chunkSize)
        {
            if (chunkSize <= 0)
            {
                throw new FormatException("The size cannot be <= 0");
            }

            for (int i = 0; i < list.Length; i += chunkSize)
            {
                yield return list.GetRange(i, Math.Min(chunkSize, list.Length - i));
            }
        }

        public static T[] GetRange<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        // credits: https://rosettacode.org/wiki/Knuth_shuffle#C.23
        public static void Shuffle<T>(this T[] array)
        {
            Random random = new Random();
            T temp = default(T);

            for (int i = 0; i < array.Length; ++i)
            {
                int j = random.Next(i, array.Length);
                temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer = null)
        {
            source.AssertNotNull("source");
            keySelector.AssertNotNull("keySelector");

            var seenKeys = new HashSet<TKey>(comparer);
            foreach (var element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                    yield return element;
            }
        }

        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, bool> eqFx, Func<TSource, int> hasher)
        {
            return
                source
                    .Distinct(LinqEqualityComparer<TSource>.Create(hasher, eqFx));
        }
    }
}
