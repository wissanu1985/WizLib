using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using Shouldly;
using Wiz.Utility.Extensions;
using Xunit;

namespace Wiz.Utility.Test.Extensions
{
    public class DateTimeExtensionsTests
    {
        [Fact]
        public void StartEndOfDay_ReturnsExpected()
        {
            var dt = new DateTime(2025, 8, 16, 10, 45, 0, DateTimeKind.Local);
            var start = dt.StartOfDay();
            var end = dt.EndOfDay();

            start.ShouldBe(new DateTime(2025, 8, 16, 0, 0, 0, DateTimeKind.Local));
            end.ShouldBe(new DateTime(2025, 8, 16, 23, 59, 59, 999, 999, DateTimeKind.Local).AddTicks(9));
        }

        [Fact]
        public void StartEndOfWeek_MondayFirst_ReturnsExpected()
        {
            var dt = new DateTime(2023, 3, 15); // Wednesday
            var start = dt.StartOfWeek(DayOfWeek.Monday);
            var end = dt.EndOfWeek(DayOfWeek.Monday);

            start.ShouldBe(new DateTime(2023, 3, 13)); // Monday
            end.Date.ShouldBe(new DateTime(2023, 3, 19)); // Sunday
            end.TimeOfDay.ShouldBe(TimeSpan.FromDays(1) - TimeSpan.FromTicks(1));
        }

        [Fact]
        public void StartEndOfMonth_LeapYear_February()
        {
            var dt = new DateTime(2024, 2, 20, 12, 0, 0, DateTimeKind.Utc);
            dt.StartOfMonth().ShouldBe(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc));
            dt.EndOfMonth().ShouldBe(new DateTime(2024, 2, 29, 23, 59, 59, 999, 999, DateTimeKind.Utc).AddTicks(9));
        }

        [Fact]
        public void StartEndOfYear_Basic()
        {
            var dt = new DateTime(2021, 6, 10, 9, 0, 0, DateTimeKind.Unspecified);
            dt.StartOfYear().ShouldBe(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
            dt.EndOfYear().ShouldBe(new DateTime(2021, 12, 31, 23, 59, 59, 999, 999, DateTimeKind.Unspecified).AddTicks(9));
        }

        [Fact]
        public void IsoWeekAndYear_KnownCases()
        {
            new DateTime(2020, 12, 31).GetIsoWeekOfYear().ShouldBe(53);
            new DateTime(2020, 12, 31).GetIsoYear().ShouldBe(2020);

            new DateTime(2021, 1, 1).GetIsoWeekOfYear().ShouldBe(53);
            new DateTime(2021, 1, 1).GetIsoYear().ShouldBe(2020);

            new DateTime(2021, 1, 4).GetIsoWeekOfYear().ShouldBe(1);
            new DateTime(2021, 1, 4).GetIsoYear().ShouldBe(2021);
        }

        [Fact]
        public void IsWeekend_Basic()
        {
            new DateTime(2023, 3, 18).IsWeekend().ShouldBeTrue(); // Saturday
            new DateTime(2023, 3, 19).IsWeekend().ShouldBeTrue(); // Sunday
            new DateTime(2023, 3, 20).IsWeekend().ShouldBeFalse(); // Monday
        }

        [Fact]
        public void AddBusinessDays_BasicAndWithHoliday()
        {
            var friday = new DateTime(2023, 3, 17); // Friday
            var nextBiz = friday.AddBusinessDays(1);
            nextBiz.Date.ShouldBe(new DateTime(2023, 3, 20)); // Monday

            var holidays = new HashSet<DateOnly> { new DateOnly(2023, 3, 20) }; // Monday holiday
            var skipHoliday = friday.AddBusinessDays(1, holidays);
            skipHoliday.Date.ShouldBe(new DateTime(2023, 3, 21)); // Tuesday
        }

        [Fact]
        public void BusinessDaysUntil_InclusiveExclusive()
        {
            var s = new DateTime(2023, 3, 13); // Mon
            var e = new DateTime(2023, 3, 17); // Fri

            s.BusinessDaysUntil(e, includeStart: true, includeEnd: true).ShouldBe(5);
            s.BusinessDaysUntil(e, includeStart: false, includeEnd: false).ShouldBe(3);
        }

        [Fact]
        public void ToIsoString_Roundtrip()
        {
            var dt = new DateTime(2025, 1, 2, 3, 4, 5, 123, DateTimeKind.Utc).AddTicks(4567);
            var s = dt.ToIsoString();
            var parsed = DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            parsed.ShouldBe(dt);
        }

        [Fact]
        public void UnixTimeConversions_MatchDateTimeOffset()
        {
            var dtUtc = new DateTime(1970, 1, 1, 0, 0, 1, DateTimeKind.Utc);
            dtUtc.ToUnixTimeSecondsUtc().ShouldBe(1);

            var dt2 = new DateTime(2020, 5, 1, 12, 0, 0, DateTimeKind.Utc);
            dt2.ToUnixTimeSecondsUtc().ShouldBe(new DateTimeOffset(dt2).ToUnixTimeSeconds());
            dt2.ToUnixTimeMillisecondsUtc().ShouldBe(new DateTimeOffset(dt2).ToUnixTimeMilliseconds());

            long s = 1_600_000_000L;
            var dto = s.ToDateTimeOffsetFromUnixSeconds();
            dto.ToUnixTimeSeconds().ShouldBe(s);

            long ms = 1_600_000_000_123L;
            var dto2 = ms.ToDateTimeOffsetFromUnixMilliseconds();
            dto2.ToUnixTimeMilliseconds().ShouldBe(ms);
        }

        [Fact]
        public void ToThaiBuddhistString_ArabicDigits()
        {
            var dt = new DateTime(2024, 1, 1);
            dt.ToThaiBuddhistString("yyyy").ShouldBe("2567");
            dt.ToThaiBuddhistString("dd/MM/yyyy").ShouldBe("01/01/2567");
        }

        [Fact]
        public void ToTimeZone_UtcToBangkok_ReturnsExpected()
        {
            var dtUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var bkk = dtUtc.ToTimeZone("SE Asia Standard Time");
            bkk.Year.ShouldBe(2024);
            bkk.Month.ShouldBe(1);
            bkk.Day.ShouldBe(1);
            bkk.Hour.ShouldBe(7);
            bkk.Minute.ShouldBe(0);
            bkk.Second.ShouldBe(0);
            bkk.Kind.ShouldBe(DateTimeKind.Unspecified);
        }

        [Fact]
        public void IsWeekend_CustomWeekend_FriSatWeekend()
        {
            var weekend = new HashSet<DayOfWeek> { DayOfWeek.Friday, DayOfWeek.Saturday };
            new DateTime(2023, 3, 17).IsWeekend(weekend).ShouldBeTrue();
            new DateTime(2023, 3, 18).IsWeekend(weekend).ShouldBeTrue();
            new DateTime(2023, 3, 19).IsWeekend(weekend).ShouldBeFalse();
            new DateTime(2023, 3, 20).IsWeekend(weekend).ShouldBeFalse();
        }

        [Fact]
        public void AddBusinessDays_NegativeAcrossWeekend_ReturnsPreviousFriday()
        {
            var monday = new DateTime(2023, 3, 20);
            var prevBiz = monday.AddBusinessDays(-1);
            prevBiz.Date.ShouldBe(new DateTime(2023, 3, 17));
        }

        [Fact]
        public void AddBusinessDays_Zero_ReturnsSame()
        {
            var dt = new DateTime(2023, 3, 15, 13, 0, 0);
            dt.AddBusinessDays(0).ShouldBe(dt);
        }

        [Fact]
        public void BusinessDaysUntil_ReversedOrder_YieldsSameAsForward()
        {
            var s = new DateTime(2023, 3, 17);
            var e = new DateTime(2023, 3, 13);
            s.BusinessDaysUntil(e, includeStart: true, includeEnd: true).ShouldBe(5);
            s.BusinessDaysUntil(e, includeStart: false, includeEnd: false).ShouldBe(3);
        }

        [Fact]
        public void ToIsoString_DateTimeOffset_Roundtrip()
        {
            var dto = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.FromHours(7)).AddTicks(4567);
            var s = dto.ToIsoString();
            var parsed = DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            parsed.ShouldBe(dto);
        }

        [Fact(Skip = "Performance smoke test; skip in CI")]
        [Trait("Category", "Benchmark")]
        public void AddBusinessDays_Benchmark()
        {
            var start = new DateTime(2025, 1, 1);
            var sw = Stopwatch.StartNew();
            var current = start;
            for (int i = 0; i < 1000; i++)
            {
                current = current.AddBusinessDays(1);
            }
            sw.Stop();
            current.ShouldNotBe(default);
            sw.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
        }
    }
}
