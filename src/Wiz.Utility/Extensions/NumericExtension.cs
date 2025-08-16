using System;
using System.Globalization;

namespace Wiz.Utility.Extensions;

/// <summary>
/// Numeric helper extension methods for common scenarios.
/// Supports int/long/double/decimal as requested.
/// </summary>
public static class NumericExtensions
{
    // Even / Odd (int, long)
    public static bool IsEven(this int value) => (value & 1) == 0;
    public static bool IsOdd(this int value) => (value & 1) != 0;
    public static bool IsEven(this long value) => (value & 1L) == 0L;
    public static bool IsOdd(this long value) => (value & 1L) != 0L;

    // Range helpers (typed overloads only)
    public static bool IsBetween(this int value, int min, int max, bool inclusive = true)
    {
        if (min > max) (min, max) = (max, min);
        return inclusive ? value >= min && value <= max : value > min && value < max;
    }

    public static bool IsBetween(this long value, long min, long max, bool inclusive = true)
    {
        if (min > max) (min, max) = (max, min);
        return inclusive ? value >= min && value <= max : value > min && value < max;
    }

    public static bool IsBetween(this double value, double min, double max, bool inclusive = true)
    {
        if (min > max) (min, max) = (max, min);
        return inclusive ? value >= min && value <= max : value > min && value < max;
    }

    public static bool IsBetween(this decimal value, decimal min, decimal max, bool inclusive = true)
    {
        if (min > max) (min, max) = (max, min);
        return inclusive ? value >= min && value <= max : value > min && value < max;
    }

    public static int Clamp(this int value, int min, int max)
    {
        if (min > max) (min, max) = (max, min);
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static long Clamp(this long value, long min, long max)
    {
        if (min > max) (min, max) = (max, min);
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static double Clamp(this double value, double min, double max)
    {
        if (min > max) (min, max) = (max, min);
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static decimal Clamp(this decimal value, decimal min, decimal max)
    {
        if (min > max) (min, max) = (max, min);
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    // Nearly equals (double, decimal)
    public static bool NearlyEquals(this double a, double b, double epsilon = 1e-10, double? relativeTolerance = null)
    {
        if (double.IsNaN(a) || double.IsNaN(b)) return false;
        if (double.IsInfinity(a) || double.IsInfinity(b)) return a.Equals(b);

        var diff = Math.Abs(a - b);
        if (relativeTolerance is > 0)
        {
            var scale = Math.Max(Math.Abs(a), Math.Abs(b));
            var tol = Math.Max(epsilon, relativeTolerance.Value * scale);
            return diff <= tol;
        }
        return diff <= epsilon;
    }

    public static bool NearlyEquals(this decimal a, decimal b, decimal epsilon = 0.0000001m)
        => Math.Abs(a - b) <= epsilon;

    // Map value from one range to another (double, decimal)
    public static double MapRange(this double value, double sourceMin, double sourceMax, double targetMin, double targetMax, bool clamp = false)
    {
        if (sourceMin == sourceMax)
            throw new ArgumentException("sourceMin and sourceMax cannot be equal.", nameof(sourceMax));
        var v = clamp ? value.Clamp(Math.Min(sourceMin, sourceMax), Math.Max(sourceMin, sourceMax)) : value;
        var ratio = (v - sourceMin) / (sourceMax - sourceMin);
        return targetMin + ratio * (targetMax - targetMin);
    }

    public static decimal MapRange(this decimal value, decimal sourceMin, decimal sourceMax, decimal targetMin, decimal targetMax, bool clamp = false)
    {
        if (sourceMin == sourceMax)
            throw new ArgumentException("sourceMin and sourceMax cannot be equal.", nameof(sourceMax));
        var v = clamp ? value.Clamp(Math.Min(sourceMin, sourceMax), Math.Max(sourceMin, sourceMax)) : value;
        var ratio = (v - sourceMin) / (sourceMax - sourceMin);
        return targetMin + ratio * (targetMax - targetMin);
    }

    // Ordinal (English): 1st, 2nd, 3rd, 4th, ... (supports negatives)
    public static string ToOrdinal(this int value, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.InvariantCulture;
        var sign = value < 0 ? "-" : string.Empty;
        var n = Math.Abs(value);
        var suffix = GetEnglishOrdinalSuffix(n);
        return sign + n.ToString(culture) + suffix;
    }

    public static string ToOrdinal(this long value, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.InvariantCulture;
        var sign = value < 0 ? "-" : string.Empty;
        var n = Math.Abs(value);
        var suffix = GetEnglishOrdinalSuffix((int)(n % 100)); // safe for suffix decision
        return sign + n.ToString(culture) + suffix;
    }

    private static string GetEnglishOrdinalSuffix(int n)
    {
        var mod100 = n % 100;
        if (mod100 is 11 or 12 or 13) return "th";
        return (n % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
    }

    // Human-readable size (bytes). Decimal units (KB/MB/GB) by default.
    public static string ToHumanSize(this long bytes, int decimals = 1, bool binary = false, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.InvariantCulture;
        var negative = bytes < 0;
        decimal value = Math.Abs(bytes);
        decimal step = binary ? 1024m : 1000m;
        string[] units = binary
            ? new[] { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" }
            : new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

        int unitIndex = 0;
        while (value >= step && unitIndex < units.Length - 1)
        {
            value /= step;
            unitIndex++;
        }

        string number = decimals > 0
            ? ((double)value).ToString($"F{decimals}", culture)
            : ((double)Math.Round(value, 0, MidpointRounding.AwayFromZero)).ToString(culture);
        var text = number + " " + units[unitIndex];
        return negative ? "-" + text : text;
    }

    // Convenience overloads for other numeric types
    public static string ToHumanSize(this int bytes, int decimals = 1, bool binary = false, CultureInfo? culture = null)
        => ((long)bytes).ToHumanSize(decimals, binary, culture);

    public static string ToHumanSize(this double bytes, int decimals = 1, bool binary = false, CultureInfo? culture = null)
        => ((long)Math.Round(bytes, MidpointRounding.AwayFromZero)).ToHumanSize(decimals, binary, culture);

    public static string ToHumanSize(this decimal bytes, int decimals = 1, bool binary = false, CultureInfo? culture = null)
        => ((long)Math.Round(bytes, 0, MidpointRounding.AwayFromZero)).ToHumanSize(decimals, binary, culture);

    // Rounding helpers (decimal)
    public static decimal RoundTo(this decimal value, int decimals, MidpointRounding mode = MidpointRounding.AwayFromZero)
        => Math.Round(value, decimals, mode);

    // Safe divide (double/decimal)
    public static double SafeDivide(this double numerator, double denominator, double defaultValue = 0.0)
        => denominator == 0.0 ? defaultValue : numerator / denominator;

    public static decimal SafeDivide(this decimal numerator, decimal denominator, decimal defaultValue = 0m)
        => denominator == 0m ? defaultValue : numerator / denominator;
}