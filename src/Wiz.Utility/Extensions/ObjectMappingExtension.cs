using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wiz.Utility.Extensions
{
    public static class ObjectMappingExtension
    {
        // Public API ---------------------------------------------------------
        public static TDestination? Adapt<TDestination>(this object? source, MappingOptions? options = null)
        {
            var opts = options ?? MappingOptions.Default;
            return (TDestination?)AdaptInternal(source, typeof(TDestination), existingDestination: null, new MappingContext(opts));
        }

        public static object? Adapt(this object? source, Type destinationType, MappingOptions? options = null)
        {
            if (destinationType is null)
                throw new ArgumentNullException(nameof(destinationType));

            var opts = options ?? MappingOptions.Default;
            return AdaptInternal(source, destinationType, existingDestination: null, new MappingContext(opts));
        }

        public static TDestination AdaptInto<TDestination>(this object? source, TDestination destination, MappingOptions? options = null)
            where TDestination : class
        {
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            var opts = options ?? MappingOptions.Default;
            AdaptInternal(source, typeof(TDestination), existingDestination: destination!, new MappingContext(opts));
            return destination;
        }

        // Core engine --------------------------------------------------------
        private static object? AdaptInternal(object? source, Type destType, object? existingDestination, MappingContext ctx)
        {
            // Null source handling
            if (source is null)
            {
                if (existingDestination is not null)
                {
                    // nothing to map onto; return existing as-is
                    return existingDestination;
                }

                // return default for reference/nullable types; default(T) for value types
                return destType.IsValueType ? Activator.CreateInstance(destType) : null;
            }

            var sourceType = source.GetType();

            // Fast path: already assignable
            if (existingDestination is null && destType.IsAssignableFrom(sourceType))
                return source;

            // Custom converter lookup
            if (ctx.Options.TryCustomConvert(source, destType, out var converted))
                return converted;

            // Value conversion path (primitives/enums/strings/Date/Guid/TimeSpan)
            if (TryChangeType(source, destType, ctx.Options, out var scalarResult))
                return scalarResult;

            // Collections
            if (TryMapCollection(source, sourceType, destType, existingDestination, ctx, out var collResult))
                return collResult;

            // Complex object mapping (POCO)
            object destination = existingDestination ?? CreateInstanceOrThrow(destType);

            // cycle detection
            if (!destType.IsValueType)
            {
                if (ctx.ReferenceTracker.TryGetValue(source, out var already))
                {
                    return already;
                }
                ctx.ReferenceTracker[source] = destination;
            }

            var maps = GetPropertyMaps(sourceType, destType, ctx.Options.CaseSensitive);

            foreach (var map in maps)
            {
                if (map.DestProp is null || map.SourceProp is null)
                    continue;

                if (ctx.Options.IsIgnored(map.DestProp.Name))
                    continue;

                try
                {
                    var srcVal = map.SourceProp.GetValue(source);

                    if (srcVal is null && ctx.Options.IgnoreNullValues)
                        continue;

                    var destPropType = map.DestProp.PropertyType;
                    object? valueToAssign;

                    if (srcVal is null)
                    {
                        valueToAssign = destPropType.IsValueType ? Activator.CreateInstance(destPropType) : null;
                    }
                    else if (destPropType.IsAssignableFrom(srcVal.GetType()))
                    {
                        valueToAssign = srcVal;
                    }
                    else if (ctx.Options.TryCustomConvert(srcVal, destPropType, out var convVal))
                    {
                        valueToAssign = convVal;
                    }
                    else if (TryChangeType(srcVal, destPropType, ctx.Options, out var changed))
                    {
                        valueToAssign = changed;
                    }
                    else if (TryMapCollection(srcVal, srcVal.GetType(), destPropType, existingDestination: map.DestProp.GetValue(destination), ctx, out var mappedColl))
                    {
                        valueToAssign = mappedColl;
                    }
                    else
                    {
                        // cannot change type directly; decide by options for scalars
                        if (IsSimpleType(srcVal.GetType()) && IsSimpleType(destPropType))
                        {
                            switch (ctx.Options.ConversionFailure)
                            {
                                case ConversionFailureBehavior.SetNullOrDefault:
                                    valueToAssign = destPropType.IsValueType ? Activator.CreateInstance(destPropType) : null;
                                    break;
                                case ConversionFailureBehavior.Skip:
                                    continue; // do not set
                                case ConversionFailureBehavior.Throw:
                                    throw new InvalidCastException($"Cannot convert value of type {srcVal.GetType()} to {destPropType} for property {map.DestProp.Name}.");
                                default:
                                    valueToAssign = destPropType.IsValueType ? Activator.CreateInstance(destPropType) : null;
                                    break;
                            }
                        }
                        else
                        {
                            // recurse for complex types
                            var existingNested = map.DestProp.GetValue(destination);
                            valueToAssign = AdaptInternal(srcVal, destPropType, existingNested, ctx);
                        }
                    }

                    // set value
                    if (map.DestProp.CanWrite && map.DestProp.SetMethod?.IsPublic == true)
                    {
                        map.DestProp.SetValue(destination, valueToAssign);
                    }
                }
                catch (Exception ex)
                {
                    if (ctx.Options.ErrorHandler is not null)
                    {
                        ctx.Options.ErrorHandler(new MappingError(sourceType, destType, map.DestProp?.Name, ex));
                    }

                    if (ctx.Options.StrictMode)
                        throw;
                }
            }

            return destination;
        }

        // Helpers ------------------------------------------------------------
        private static bool TryMapCollection(object source, Type sourceType, Type destType, object? existingDestination, MappingContext ctx, out object? result)
        {
            result = null;

            var srcEnumerable = GetEnumerableElementType(sourceType);
            var destEnumerable = GetEnumerableElementType(destType);
            if (srcEnumerable is null || destEnumerable is null)
                return false;

            // Prepare a materialized list of source elements
            var srcItems = ((System.Collections.IEnumerable)source).Cast<object?>().ToList();

            // Map each element
            var mappedItems = new List<object?>(srcItems.Count);
            foreach (var item in srcItems)
            {
                mappedItems.Add(AdaptInternal(item, destEnumerable, existingDestination: null, ctx));
            }

            if (destType.IsArray)
            {
                var arr = Array.CreateInstance(destEnumerable, mappedItems.Count);
                for (int i = 0; i < mappedItems.Count; i++)
                {
                    arr.SetValue(mappedItems[i], i);
                }
                result = arr;
                return true;
            }

            // Try to fill existing destination collection if available
            if (existingDestination is System.Collections.IList list && IsListOfType(list.GetType(), destEnumerable))
            {
                TryClearList(list);
                foreach (var mi in mappedItems)
                    list.Add(mi);
                result = list;
                return true;
            }

            // Instantiate List<destEnumerable>
            var listType = typeof(List<>).MakeGenericType(destEnumerable);
            var newList = (System.Collections.IList)Activator.CreateInstance(listType)!;
            foreach (var mi in mappedItems)
                newList.Add(mi);
            result = newList;
            return true;
        }

        private static bool IsListOfType(Type listType, Type elementType)
        {
            if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
                return listType.GetGenericArguments()[0] == elementType;
            return listType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>) && i.GetGenericArguments()[0] == elementType);
        }

        private static void TryClearList(System.Collections.IList list)
        {
            try
            {
                list.Clear();
            }
            catch
            {
                // ignore if not supported
            }
        }

        private static Type? GetEnumerableElementType(Type type)
        {
            if (type == typeof(string))
                return null;

            if (type.IsArray)
                return type.GetElementType();

            var enumerableIface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerableIface?.GetGenericArguments()[0];
        }

        private static object CreateInstanceOrThrow(Type t)
        {
            try
            {
                return Activator.CreateInstance(t) ?? throw new InvalidOperationException($"Unable to create instance of type {t}");
            }
            catch (MissingMethodException ex)
            {
                throw new InvalidOperationException($"Type {t} must have a public parameterless constructor to be mapped.", ex);
            }
        }

        private static bool TryChangeType(object? value, Type destinationType, MappingOptions options, out object? result)
        {
            result = null;
            if (destinationType is null)
                return false;

            if (value is null)
            {
                result = destinationType.IsValueType ? Activator.CreateInstance(destinationType) : null;
                return true;
            }

            var srcType = value.GetType();
            if (destinationType.IsAssignableFrom(srcType))
            {
                result = value;
                return true;
            }

            var underlying = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

            try
            {
                // Enums
                if (underlying.IsEnum)
                {
                    if (value is string s)
                    {
                        if (Enum.TryParse(underlying, s, ignoreCase: true, out var enumParsed))
                        {
                            result = enumParsed;
                            return true;
                        }
                        return false;
                    }
                    result = Enum.ToObject(underlying, System.Convert.ChangeType(value, Enum.GetUnderlyingType(underlying))!);
                    return true;
                }

                // Guid
                if (underlying == typeof(Guid))
                {
                    if (value is string gs && Guid.TryParse(gs, out var g))
                    {
                        result = g;
                        return true;
                    }
                    return false;
                }

                // DateTime / DateTimeOffset / TimeSpan from string (with common formats)
                if (underlying == typeof(DateTime))
                {
                    if (value is string ds)
                    {
                        var fmts = options.DateTimeFormats ?? MappingOptions.DefaultDateTimeFormats;
                        if (DateTime.TryParseExact(ds, fmts, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtx))
                        {
                            result = dtx;
                            return true;
                        }
                        if (DateTime.TryParse(ds, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtp))
                        {
                            result = dtp;
                            return true;
                        }
                        return false;
                    }
                }
                if (underlying == typeof(DateTimeOffset))
                {
                    if (value is string dos)
                    {
                        var fmts = options.DateTimeFormats ?? MappingOptions.DefaultDateTimeFormats;
                        if (DateTimeOffset.TryParseExact(dos, fmts, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtoe))
                        {
                            result = dtoe;
                            return true;
                        }
                        if (DateTimeOffset.TryParse(dos, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtop))
                        {
                            result = dtop;
                            return true;
                        }
                        return false;
                    }
                }
                if (underlying == typeof(TimeSpan))
                {
                    if (value is string ts && TimeSpan.TryParse(ts, out var tsp))
                    {
                        result = tsp;
                        return true;
                    }
                }

                // Boolean from string "1"/"0" / "true"/"false"
                if (underlying == typeof(bool) && value is string bs)
                {
                    if (bool.TryParse(bs, out var bv)) { result = bv; return true; }
                    if (bs == "1") { result = true; return true; }
                    if (bs == "0") { result = false; return true; }
                    return false;
                }

                // Numeric conversions from string allowing thousands separators (InvariantCulture)
                if (value is string str && IsNumericType(underlying))
                {
                    var style = System.Globalization.NumberStyles.Number;
                    var ci = System.Globalization.CultureInfo.InvariantCulture;
                    if (underlying == typeof(decimal))
                    {
                        if (decimal.TryParse(str, style, ci, out var dec)) { result = dec; return true; }
                        return false;
                    }
                    if (underlying == typeof(double))
                    {
                        if (double.TryParse(str, style, ci, out var dbl)) { result = dbl; return true; }
                        return false;
                    }
                    if (underlying == typeof(float))
                    {
                        if (float.TryParse(str, style, ci, out var fl)) { result = fl; return true; }
                        return false;
                    }
                    if (underlying == typeof(long))
                    {
                        if (long.TryParse(str, style, ci, out var l)) { result = l; return true; }
                        return false;
                    }
                    if (underlying == typeof(int))
                    {
                        if (int.TryParse(str, style, ci, out var i)) { result = i; return true; }
                        return false;
                    }
                }

                // String destination
                if (underlying == typeof(string))
                {
                    result = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                }

                // Numeric and primitive conversions
                result = System.Convert.ChangeType(value, underlying, System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        private static bool IsNumericType(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            return t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) ||
                   t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) ||
                   t == typeof(float) || t == typeof(double) || t == typeof(decimal);
        }

        private static bool IsSimpleType(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(Guid) ||
                   t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(TimeSpan);
        }

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type Src, Type Dest, bool Case), PropertyMap[]> _mapCache = new();

        private static PropertyMap[] GetPropertyMaps(Type src, Type dest, bool caseSensitive)
        {
            return _mapCache.GetOrAdd((src, dest, caseSensitive), static key =>
            {
                var (s, d, cs) = key;
                var sProps = s.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                              .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                              .ToArray();
                var dProps = d.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                              .Where(p => p.CanWrite && p.SetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0)
                              .ToArray();

                var comparer = cs ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
                var dDict = dProps.ToDictionary(p => p.Name, comparer);

                var maps = new List<PropertyMap>(Math.Min(sProps.Length, dProps.Length));
                foreach (var sp in sProps)
                {
                    if (dDict.TryGetValue(sp.Name, out var dp))
                    {
                        maps.Add(new PropertyMap(sp, dp));
                    }
                }
                return maps.ToArray();
            });
        }

        private sealed class MappingContext
        {
            public MappingContext(MappingOptions options)
            {
                Options = options;
            }

            public MappingOptions Options { get; }
            public Dictionary<object, object> ReferenceTracker { get; } = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new();
            private ReferenceEqualityComparer() { }
            public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        private readonly record struct PropertyMap(System.Reflection.PropertyInfo SourceProp, System.Reflection.PropertyInfo DestProp);
    }

    // Options and error contracts -------------------------------------------
    public enum ConversionFailureBehavior
    {
        SetNullOrDefault = 0,
        Skip = 1,
        Throw = 2
    }
    public sealed class MappingOptions
    {
        private readonly ISet<string> _ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static MappingOptions Default { get; } = new MappingOptions();

        public bool CaseSensitive { get; init; } = false;
        public bool IgnoreNullValues { get; init; } = false;
        public bool StrictMode { get; init; } = false;

        // Behavior when simple-to-simple conversion fails
        public ConversionFailureBehavior ConversionFailure { get; init; } = ConversionFailureBehavior.SetNullOrDefault;

        // DateTime parsing formats (fallback to DefaultDateTimeFormats when null)
        public string[]? DateTimeFormats { get; init; }
        public static readonly string[] DefaultDateTimeFormats = new[]
        {
            "O",              // Round-trip
            "yyyy-MM-dd",    // 2025-08-16
            "yyyy/MM/dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
        };

        public Action<MappingError>? ErrorHandler { get; init; }

        // Custom converters registry (BCL-only): map by (Src, Dest)
        private readonly Dictionary<(Type Src, Type Dest), Func<object?, object?>> _converters = new();

        public MappingOptions Ignore(string destinationPropertyName)
        {
            if (string.IsNullOrWhiteSpace(destinationPropertyName))
                throw new ArgumentException("Property name cannot be null/empty.", nameof(destinationPropertyName));
            _ignored.Add(destinationPropertyName);
            return this;
        }

        internal bool IsIgnored(string destinationPropertyName)
            => _ignored.Contains(destinationPropertyName);

        public MappingOptions AddConverter<TSrc, TDest>(Func<TSrc?, TDest?> converter)
        {
            if (converter is null) throw new ArgumentNullException(nameof(converter));
            _converters[(typeof(TSrc), typeof(TDest))] = obj => converter((TSrc?)obj);
            return this;
        }

        internal bool TryCustomConvert(object? value, Type destType, out object? result)
        {
            result = null;
            var srcType = value?.GetType() ?? typeof(object);

            // Exact match
            if (_converters.TryGetValue((srcType, destType), out var exact))
            {
                result = exact(value);
                return true;
            }

            // Fallback: any converter where registered Src is assignable from actual srcType and registered Dest equals destType
            foreach (var kvp in _converters)
            {
                var (registeredSrc, registeredDest) = kvp.Key;
                if (registeredDest == destType && registeredSrc.IsAssignableFrom(srcType))
                {
                    result = kvp.Value(value);
                    return true;
                }
            }

            return false;
        }
    }

    public readonly record struct MappingError(Type SourceType, Type DestinationType, string? DestinationMember, Exception Exception);
}