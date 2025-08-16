using System.Globalization;
using Shouldly;
using Wiz.Utility.Extensions;

namespace Wiz.Utility.Test.Extensions;

public class NumericExtensionsTests
{
    [Fact]
    public void IsEvenOdd_Int_Long()
    {
        0.IsEven().ShouldBeTrue();
        1.IsOdd().ShouldBeTrue();
        (-2).IsEven().ShouldBeTrue();
        5L.IsOdd().ShouldBeTrue();
        10L.IsEven().ShouldBeTrue();
    }

    [Fact]
    public void IsBetween_Typed_Overloads_InclusiveExclusive()
    {
        5.IsBetween(1, 5).ShouldBeTrue();
        5.IsBetween(1, 5, inclusive: false).ShouldBeFalse();
        2.5d.IsBetween(2.0, 3.0).ShouldBeTrue();
        2.0m.IsBetween(2.0m, 3.0m, inclusive: false).ShouldBeFalse();
        // Swapped bounds
        5.IsBetween(10, 1).ShouldBeTrue();
    }

    [Fact]
    public void Clamp_Typed_Overloads()
    {
        5.Clamp(1, 10).ShouldBe(5);
        0.Clamp(1, 10).ShouldBe(1);
        11.Clamp(1, 10).ShouldBe(10);
        2.5d.Clamp(3.0, 9.0).ShouldBe(3.0);
        9.1d.Clamp(3.0, 9.0).ShouldBe(9.0);
        5m.Clamp(10m, 1m).ShouldBe(5m); // swapped bounds handled
    }

    [Fact]
    public void NearlyEquals_Double_Decimal()
    {
        (0.1 + 0.2).NearlyEquals(0.3, epsilon: 1e-9).ShouldBeTrue();
        1.0.NearlyEquals(1.1, epsilon: 1e-12).ShouldBeFalse();

        (1.0000000m).NearlyEquals(1.00000005m, 0.000001m).ShouldBeTrue();
        (1.0001m).NearlyEquals(1.0003m, 0.00001m).ShouldBeFalse();
    }

    [Fact]
    public void MapRange_Double_Decimal_WithClamp()
    {
        // Map 5 from [0,10] to [0,100] => 50
        5d.MapRange(0, 10, 0, 100).ShouldBe(50d);
        // Clamp before mapping when source value outside source range
        (-2d).MapRange(0, 10, 0, 100, clamp: true).ShouldBe(0d);

        5m.MapRange(0m, 10m, 0m, 100m).ShouldBe(50m);
        12m.MapRange(0m, 10m, 0m, 100m, clamp: true).ShouldBe(100m);
    }

    [Fact]
    public void ToOrdinal_Int_Long()
    {
        1.ToOrdinal().ShouldBe("1st");
        2.ToOrdinal().ShouldBe("2nd");
        3.ToOrdinal().ShouldBe("3rd");
        4.ToOrdinal().ShouldBe("4th");
        11.ToOrdinal().ShouldBe("11th");
        12.ToOrdinal().ShouldBe("12th");
        13.ToOrdinal().ShouldBe("13th");
        21.ToOrdinal().ShouldBe("21st");
        (-1).ToOrdinal().ShouldBe("-1st");

        101L.ToOrdinal().ShouldBe("101st");
    }

    [Fact]
    public void ToHumanSize_DefaultDecimalUnits()
    {
        // default decimals = 1, decimal units (KB/MB/GB)
        0L.ToHumanSize().ShouldBe("0.0 B");
        999L.ToHumanSize().ShouldBe("999.0 B");
        1000L.ToHumanSize().ShouldBe("1.0 KB");
        1536L.ToHumanSize().ShouldBe("1.5 KB");
        1048576L.ToHumanSize().ShouldBe("1.0 MB");
        (-2048L).ToHumanSize().ShouldBe("-2.0 KB");

        // convenience overloads
        1536.ToHumanSize().ShouldBe("1.5 KB");
        1536.0.ToHumanSize().ShouldBe("1.5 KB");
        1536m.ToHumanSize().ShouldBe("1.5 KB");

        // binary units
        1536L.ToHumanSize(binary: true).ShouldBe("1.5 KiB");
    }

    [Fact]
    public void RoundTo_SafeDivide()
    {
        1.2345m.RoundTo(2).ShouldBe(1.23m);
        1.2355m.RoundTo(2).ShouldBe(1.24m);

        10.0.SafeDivide(0.0, defaultValue: -1.0).ShouldBe(-1.0);
        10.0.SafeDivide(2.0).ShouldBe(5.0);
        10m.SafeDivide(0m, defaultValue: -1m).ShouldBe(-1m);
        10m.SafeDivide(2m).ShouldBe(5m);
    }
}
