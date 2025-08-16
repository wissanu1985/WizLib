# DateTime extensions (Wiz.Utility)

Last updated: 2025-08-16

This module provides culture-safe helpers for common date/time tasks: start/end boundaries, ISO week/year, business-day arithmetic, ISO8601/Unix conversions, Thai Buddhist formatting, and time zone conversion.

## API summary

- Start/end boundaries
  - `StartOfDay()`, `EndOfDay()`
  - `StartOfWeek(DayOfWeek first = Monday)`, `EndOfWeek(DayOfWeek first = Monday)`
  - `StartOfMonth()`, `EndOfMonth()`
  - `StartOfYear()`, `EndOfYear()`
- ISO week/year
  - `GetIsoWeekOfYear()`, `GetIsoYear()`
- Business days
  - `IsWeekend()`; configurable: `IsWeekend(ISet<DayOfWeek> weekendDays)`
  - `AddBusinessDays(int days, ISet<DateOnly>? holidays = null)`
  - `BusinessDaysUntil(DateTime end, ISet<DateOnly>? holidays = null, bool includeStart = false, bool includeEnd = false)`
- ISO/Unix/time zones
  - `ToIsoString()` (Round-trip, ISO 8601 "o")
  - `ToUnixTimeSecondsUtc()`, `ToUnixTimeMillisecondsUtc()`
  - `long.ToDateTimeOffsetFromUnixSeconds() / FromUnixMilliseconds()`
  - `ToTimeZone(string timeZoneId)` (uses `DateTime.Kind`)
- Thai Buddhist calendar
  - `ToThaiBuddhistString(string format = "dd/MM/yyyy")` (always Arabic digits)

## Usage examples

```csharp
using Wiz.Utility.Extensions;
using System.Globalization;

var now = DateTime.Now;
var sod = now.StartOfDay();
var eom = now.EndOfMonth();

int isoWeek = now.GetIsoWeekOfYear();
int isoYear = now.GetIsoYear();

// Business days (Mon–Fri); skip Thai New Year (example)
var holidays = new HashSet<DateOnly> { new(2025, 4, 14), new(2025, 4, 15) };
var due = new DateTime(2025, 4, 11).AddBusinessDays(3, holidays);

// ISO 8601
string iso = DateTime.UtcNow.ToIsoString();
var parsed = DateTime.Parse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

// Unix time
long s = DateTime.UtcNow.ToUnixTimeSecondsUtc();
var dto = s.ToDateTimeOffsetFromUnixSeconds();

// Time zone
var bkk = DateTime.UtcNow.ToTimeZone("SE Asia Standard Time");

// Thai Buddhist calendar (Arabic digits)
string th = new DateTime(2024, 1, 1).ToThaiBuddhistString("dd MMM yyyy"); // "01 ม.ค. 2567"
```

## Notes & behavior
- All boundary methods preserve `DateTime.Kind`.
- ISO week/year based on `System.Globalization.ISOWeek` (ISO-8601; Monday as first day).
- Business-day helpers consider Saturday/Sunday as weekend by default and optionally exclude `holidays` as `DateOnly`.
- `ToTimeZone` uses `TimeZoneInfo.ConvertTime`. If `DateTime.Kind` is `Utc`, conversion is from UTC; if `Local`, from local; else treated as unspecified.
- `ToThaiBuddhistString` forces Thai Buddhist calendar and replaces Thai digits with Arabic digits for consistency across platforms.

## Limitations
- Business days do not account for partial/half days.
- `ToTimeZone` requires a valid Windows time zone ID on Windows (IANA IDs require mapping).

## References
- Microsoft Learn — Standard date and time format strings: https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings
- Microsoft Learn — ISOWeek: https://learn.microsoft.com/en-us/dotnet/api/system.globalization.isoweek
- Microsoft Learn — DateOnly/TimeOnly: https://learn.microsoft.com/en-us/dotnet/standard/datetime/how-to-use-dateonly-timeonly
- Microsoft Learn — Converting between time zones: https://learn.microsoft.com/en-us/dotnet/standard/datetime/converting-between-time-zones
- Microsoft Learn — ThaiBuddhistCalendar: https://learn.microsoft.com/en-us/dotnet/api/system.globalization.thaibuddhistcalendar
- Microsoft Learn — DateTimeOffset Unix time: https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset.tounixtimeseconds , https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset.fromunixtimeseconds
- Business days algorithms (discussion): StackOverflow: https://stackoverflow.com/questions/279296 (CC BY-SA 4.0)
