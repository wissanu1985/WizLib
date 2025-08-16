using System;
using System.Collections.Generic;
using System.Globalization;

namespace Wiz.Utility.Extensions;

/// <summary>
/// Date and time helper extension methods.
/// Focused on culture-safe calculations and ISO/Unix utilities.
/// </summary>
public static class DateTimeExtensions
{
    // Start/End of Day
    public static DateTime StartOfDay(this DateTime dt) => dt.Date;

    public static DateTime EndOfDay(this DateTime dt)
        => dt.Date.AddDays(1).AddTicks(-1);

    // Start/End of Week (default Monday per ISO)
    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
    {
        int diff = (7 + (dt.DayOfWeek - firstDayOfWeek)) % 7;
        return dt.Date.AddDays(-diff);
    }

    public static DateTime EndOfWeek(this DateTime dt, DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
        => dt.StartOfWeek(firstDayOfWeek).AddDays(7).AddTicks(-1);

    // Start/End of Month
    public static DateTime StartOfMonth(this DateTime dt)
        => new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, dt.Kind);

    public static DateTime EndOfMonth(this DateTime dt)
        => dt.StartOfMonth().AddMonths(1).AddTicks(-1);

    // Start/End of Year
    public static DateTime StartOfYear(this DateTime dt)
        => new DateTime(dt.Year, 1, 1, 0, 0, 0, dt.Kind);

    public static DateTime EndOfYear(this DateTime dt)
        => dt.StartOfYear().AddYears(1).AddTicks(-1);

    // ISO week/year
    public static int GetIsoWeekOfYear(this DateTime dt)
        => ISOWeek.GetWeekOfYear(dt);

    public static int GetIsoYear(this DateTime dt)
        => ISOWeek.GetYear(dt);

    // Weekend checks
    public static bool IsWeekend(this DateTime dt)
        => dt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    public static bool IsWeekend(this DateTime dt, ISet<DayOfWeek> weekendDays)
    {
        if (weekendDays is null || weekendDays.Count == 0)
            return IsWeekend(dt);
        return weekendDays.Contains(dt.DayOfWeek);
    }

    // Business days (Mon-Fri by default) with optional holidays (DateOnly)
    public static DateTime AddBusinessDays(this DateTime dt, int days, ISet<DateOnly>? holidays = null)
    {
        if (days == 0) return dt;
        int step = days > 0 ? 1 : -1;
        int remaining = Math.Abs(days);
        var d = dt;

        while (remaining > 0)
        {
            d = d.AddDays(step);
            if (IsBusinessDay(d, holidays))
                remaining--;
        }
        return d;
    }

    public static int BusinessDaysUntil(this DateTime start, DateTime end, ISet<DateOnly>? holidays = null, bool includeStart = false, bool includeEnd = false)
    {
        // Normalize order
        var s = start.Date;
        var e = end.Date;
        if (s > e) (s, e) = (e, s);

        int count = 0;
        for (var d = s; d <= e; d = d.AddDays(1))
        {
            if (!includeStart && d == s) continue;
            if (!includeEnd && d == e) continue;
            if (IsBusinessDay(d, holidays)) count++;
        }
        return count;
    }

    private static bool IsBusinessDay(DateTime date, ISet<DateOnly>? holidays)
    {
        if (date.IsWeekend()) return false;
        if (holidays is { Count: > 0 })
        {
            var d = DateOnly.FromDateTime(date);
            if (holidays.Contains(d)) return false;
        }
        return true;
    }

    // ISO 8601 format helpers
    public static string ToIsoString(this DateTime dt)
        => dt.ToString("o", CultureInfo.InvariantCulture);

    public static string ToIsoString(this DateTimeOffset dto)
        => dto.ToString("o", CultureInfo.InvariantCulture);

    // Unix time helpers
    public static long ToUnixTimeSecondsUtc(this DateTime dt)
        => new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeSeconds();

    public static long ToUnixTimeMillisecondsUtc(this DateTime dt)
        => new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeMilliseconds();

    public static DateTimeOffset ToDateTimeOffsetFromUnixSeconds(this long seconds)
        => DateTimeOffset.FromUnixTimeSeconds(seconds);

    public static DateTimeOffset ToDateTimeOffsetFromUnixMilliseconds(this long milliseconds)
        => DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);

    // Time zone conversion (uses DateTime.Kind to infer source zone when possible)
    public static DateTime ToTimeZone(this DateTime dt, string timeZoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTime(dt, tz);
    }

    // Thai Buddhist Calendar formatting (Arabic digits)
    public static string ToThaiBuddhistString(this DateTime dt, string format = "dd/MM/yyyy")
    {
        var th = new CultureInfo("th-TH");
        // Ensure Thai Buddhist calendar
        th.DateTimeFormat.Calendar = new ThaiBuddhistCalendar();
        var s = dt.ToString(format, th);
        return ReplaceThaiDigitsWithAscii(s);
    }

    private static string ReplaceThaiDigitsWithAscii(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        Span<char> buffer = stackalloc char[text.Length];
        int i = 0;
        foreach (var ch in text)
        {
            if (ch >= '\u0E50' && ch <= '\u0E59')
            {
                buffer[i++] = (char)('0' + (ch - '\u0E50'));
            }
            else
            {
                buffer[i++] = ch;
            }
        }
        return new string(buffer.Slice(0, i));
    }
}