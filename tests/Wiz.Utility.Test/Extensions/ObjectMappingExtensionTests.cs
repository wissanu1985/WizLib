using System;
using System.Collections.Generic;
using Shouldly;
using Wiz.Utility.Extensions;
using Xunit;

namespace Wiz.Utility.Test.Extensions;

public class ObjectMappingExtensionTests
{
    private sealed class SimpleSrc
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Age { get; set; }
    }

    private enum Color { Red = 1, Green = 2, Blue = 3 }
    private sealed class EnumSrc { public string? Shade { get; set; } }
    private sealed class EnumDest { public Color Shade { get; set; } }

    private sealed class CtorSelectSrc { public string? Id { get; set; } public string? When { get; set; } }
    private sealed class CtorSelectDest
    {
        public int Id { get; }
        public DateTime When { get; }
        public CtorSelectDest(int id, DateTime when) { Id = id; When = when; }
        public CtorSelectDest(int id) { Id = id; When = default; }
    }

    [Fact]
    public void Enum_String_Name_Parses_To_Enum_CaseInsensitive()
    {
        var src = new EnumSrc { Shade = "bLuE" };
        var dest = src.Adapt<EnumDest>();
        dest.ShouldNotBeNull();
        dest!.Shade.ShouldBe(Color.Blue);
    }

    [Fact]
    public void Ctor_Selection_Falls_Back_On_Throwing_ConversionFailure()
    {
        var src = new CtorSelectSrc { Id = "5", When = "not-a-date" };
        var opts = new MappingOptions { ConversionFailure = ConversionFailureBehavior.Throw };
        var dest = src.Adapt<CtorSelectDest>(opts);
        dest.ShouldNotBeNull();
        dest!.Id.ShouldBe(5);
        dest.When.ShouldBe(default);
    }

    // Additional constructor-based destination types for targeted tests
    private sealed class CtorOnlyDest
    {
        public int Id { get; }
        public string? Name { get; }
        public CtorOnlyDest(int id, string? name)
        {
            Id = id;
            Name = name;
        }
    }

    private sealed class ThrowingCtorDest
    {
        public string? Name { get; }
        public ThrowingCtorDest(string? name)
        {
            // Simulate ctor failure to force CreateInstanceWithCtorIfNeeded to give up
            throw new InvalidOperationException("ctor fail");
        }
    }

    private sealed class CtorSource
    {
        public string? Name { get; set; }
        public string? Id { get; set; }
    }

    [Fact]
    public void Adapt_FastPath_When_Destination_AssignableFrom_Source_Returns_Same_Instance()
    {
        var s = "abc";
        var result = s.Adapt<string>();
        ReferenceEquals(s, result).ShouldBeTrue();
    }

    [Fact]
    public void AdaptInto_With_Null_Source_Returns_Existing_Unchanged()
    {
        var dest = new { X = 1 };
        object? src = null;
        var mapped = src.AdaptInto(dest);
        ReferenceEquals(dest, mapped).ShouldBeTrue();
        mapped.X.ShouldBe(1);
    }

    [Fact]
    public void Adapt_Uses_Constructor_When_No_Parameterless_Ctor()
    {
        var src = new CtorSource { Name = "Zed", Id = "10" }; // Id string -> int via TryChangeType
        var dest = src.Adapt<CtorOnlyDest>();
        dest.ShouldNotBeNull();
        dest!.Id.ShouldBe(10);
        dest.Name.ShouldBe("Zed");
    }

    [Fact]
    public void Adapt_NoParameterlessCtor_And_Failing_Ctor_Throws_ParameterlessRequired()
    {
        var src = new { Name = "X" };
        var ex = Should.Throw<InvalidOperationException>(() => src.Adapt<ThrowingCtorDest>());
        ex.Message.ShouldContain("must have a public parameterless constructor");
    }

    private sealed class BoolSrc { public string? B { get; set; } }
    private sealed class BoolDest { public bool B { get; set; } }

    private sealed class GuidSrc { public string? G { get; set; } }
    private sealed class GuidDest { public Guid G { get; set; } }

    private sealed class DtoSrc { public int N { get; set; } public TimeSpan? T { get; set; } }
    private sealed class DtoDest { public string? N { get; set; } public string? T { get; set; } }

    private class Animal { public string? Name { get; set; } }
    private sealed class Dog : Animal { public int Age { get; set; } }
    private sealed class AnimalDest { public string? Name { get; set; } }

    private sealed class NumSrc { public string? S { get; set; } }
    private sealed class NumDestInt { public int S { get; set; } }
    private sealed class NumDestDecimal { public decimal S { get; set; } }
    private sealed class DateSrc { public string? When { get; set; } }
    private sealed class DateDest { public DateTime When { get; set; } }

    [Fact]
    public void ConversionFailure_Default_SetNullOrDefault_OnBadScalar()
    {
        var src = new SimpleSrc { Age = "not-a-number" };
        var dest = src.Adapt<SimpleDest>();
        dest.ShouldNotBeNull();
        dest!.Age.ShouldBe(0); // default(int)
    }

    [Fact]
    public void ConversionFailure_Skip_KeepsExistingValue()
    {
        var src = new SimpleSrc { Age = "bad" };
        var dest = new SimpleDest { Age = 123 };
        var opts = new MappingOptions { ConversionFailure = ConversionFailureBehavior.Skip };
        src.AdaptInto(dest, opts);
        dest.Age.ShouldBe(123);
    }

    [Fact]
    public void ConversionFailure_Throw_Raises_InvalidCast()
    {
        var src = new SimpleSrc { Age = "bad" };
        var opts = new MappingOptions { ConversionFailure = ConversionFailureBehavior.Throw };
        Should.Throw<InvalidCastException>(() => src.Adapt<SimpleDest>(opts));
    }

    [Fact]
    public void Numeric_String_With_Thousands_Separator_Parses_To_Int()
    {
        var src = new NumSrc { S = "1,234" };
        var dest = src.Adapt<NumDestInt>();
        dest.ShouldNotBeNull();
        dest!.S.ShouldBe(1234);
    }

    [Fact]
    public void Numeric_String_With_Thousands_And_Decimals_Parses_To_Decimal()
    {
        var src = new NumSrc { S = "12,345.67" };
        var dest = src.Adapt<NumDestDecimal>();
        dest.ShouldNotBeNull();
        dest!.S.ShouldBe(12345.67m);
    }

    [Fact]
    public void Date_String_Common_Formats_Parse_To_DateTime()
    {
        // yyyy-MM-dd
        var src1 = new DateSrc { When = "2025-08-16" };
        var d1 = src1.Adapt<DateDest>();
        d1.ShouldNotBeNull();
        d1!.When.Year.ShouldBe(2025);
        d1.When.Month.ShouldBe(8);
        d1.When.Day.ShouldBe(16);

        // MM/dd/yyyy
        var src2 = new DateSrc { When = "08/16/2025" };
        var d2 = src2.Adapt<DateDest>();
        d2.ShouldNotBeNull();
        d2!.When.Year.ShouldBe(2025);
        d2.When.Month.ShouldBe(8);
        d2.When.Day.ShouldBe(16);

        // dd/MM/yyyy
        var src3 = new DateSrc { When = "16/08/2025" };
        var d3 = src3.Adapt<DateDest>();
        d3.ShouldNotBeNull();
        d3!.When.Year.ShouldBe(2025);
        d3.When.Month.ShouldBe(8);
        d3.When.Day.ShouldBe(16);
    }

    [Fact]
    public void Date_String_RoundTrip_O_Parse_Format()
    {
        var iso = new DateTime(2025, 8, 16, 12, 34, 56, DateTimeKind.Utc).ToString("O");
        var src = new DateSrc { When = iso };
        var dest = src.Adapt<DateDest>();
        dest.ShouldNotBeNull();
        dest!.When.Year.ShouldBe(2025);
        dest.When.Month.ShouldBe(8);
        dest.When.Day.ShouldBe(16);
        dest.When.Hour.ShouldBe(12);
        dest.When.Minute.ShouldBe(34);
        dest.When.Second.ShouldBe(56);
    }
    private sealed class SimpleDest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private sealed class NestedSrc
    {
        public SimpleSrc? Child { get; set; }
        public List<SimpleSrc>? Items { get; set; }
    }

    private sealed class NestedDest
    {
        public SimpleDest? Child { get; set; }
        public List<SimpleDest>? Items { get; set; }
    }

    private sealed class WithArraySrc
    {
        public SimpleSrc[]? Arr { get; set; }
    }

    private sealed class WithArrayDest
    {
        public SimpleDest[]? Arr { get; set; }
    }

    private sealed class CaseSrc
    {
        public string? firstName { get; set; }
    }

    private sealed class CaseDest
    {
        public string? FirstName { get; set; }
    }

    private sealed class Node
    {
        public string? Name { get; set; }
        public Node? Next { get; set; }
    }

    [Fact]
    public void Adapt_Simple_MapsByName_WithScalarConversion()
    {
        var src = new SimpleSrc { Id = 5, Name = "Alice", Age = "42" };

        var dest = src.Adapt<SimpleDest>();

        dest.ShouldNotBeNull();
        dest!.Id.ShouldBe(5);
        dest.Name.ShouldBe("Alice");
        dest.Age.ShouldBe(42);
    }

    [Fact]
    public void Adapt_Nested_And_Collections()
    {
        var src = new NestedSrc
        {
            Child = new SimpleSrc { Id = 1, Name = "Bob", Age = "21" },
            Items = new List<SimpleSrc>
            {
                new SimpleSrc { Id = 2, Name = "C1", Age = "30" },
                new SimpleSrc { Id = 3, Name = "C2", Age = "31" },
            }
        };

        var dest = src.Adapt<NestedDest>();

        dest.ShouldNotBeNull();
        dest!.Child.ShouldNotBeNull();
        dest.Child!.Id.ShouldBe(1);
        dest.Child.Name.ShouldBe("Bob");
        dest.Child.Age.ShouldBe(21);

        dest.Items.ShouldNotBeNull();
        dest.Items!.Count.ShouldBe(2);
        dest.Items[0].Id.ShouldBe(2);
        dest.Items[0].Age.ShouldBe(30);
        dest.Items[1].Id.ShouldBe(3);
        dest.Items[1].Age.ShouldBe(31);
    }

    [Fact]
    public void Adapt_Arrays_Supported()
    {
        var src = new WithArraySrc
        {
            Arr = new[]
            {
                new SimpleSrc { Id = 10, Name = "A", Age = "1" },
                new SimpleSrc { Id = 11, Name = "B", Age = "2" },
            }
        };

        var dest = src.Adapt<WithArrayDest>();

        dest.ShouldNotBeNull();
        dest!.Arr.ShouldNotBeNull();
        dest.Arr!.Length.ShouldBe(2);
        dest.Arr[0].Id.ShouldBe(10);
        dest.Arr[0].Age.ShouldBe(1);
        dest.Arr[1].Id.ShouldBe(11);
        dest.Arr[1].Age.ShouldBe(2);
    }

    [Fact]
    public void Adapt_CaseInsensitive_ByDefault()
    {
        var src = new CaseSrc { firstName = "z" };
        var dest = src.Adapt<CaseDest>();
        dest.ShouldNotBeNull();
        dest!.FirstName.ShouldBe("z");
    }

    [Fact]
    public void Adapt_CaseSensitive_Option_Disables_Mismatch()
    {
        var src = new CaseSrc { firstName = "z" };
        var opts = new MappingOptions { CaseSensitive = true };
        var dest = src.Adapt<CaseDest>(opts);
        dest.ShouldNotBeNull();
        dest!.FirstName.ShouldBeNull();
    }

    [Fact]
    public void AdaptInto_UpdatesExisting_WhenIgnoreNullValues_IsTrue()
    {
        var src = new SimpleSrc { Name = null, Age = "99" };
        var dest = new SimpleDest { Id = 7, Name = "keep", Age = 1 };
        var opts = new MappingOptions { IgnoreNullValues = true };

        src.AdaptInto(dest, opts);

        dest.Id.ShouldBe(7); // unchanged (no Id in src)
        dest.Name.ShouldBe("keep"); // null ignored
        dest.Age.ShouldBe(99);
    }

    [Fact]
    public void Ignore_Property_ByName()
    {
        var src = new SimpleSrc { Id = 1, Name = "won't copy", Age = "5" };
        var opts = new MappingOptions().Ignore(nameof(SimpleDest.Name));

        var dest = src.Adapt<SimpleDest>(opts)!;

        dest.Id.ShouldBe(1);
        dest.Name.ShouldBeNull();
        dest.Age.ShouldBe(5);
    }

    [Fact]
    public void Custom_Converter_Is_Used()
    {
        var src = new SimpleSrc { Id = 1, Name = "X", Age = "15" };
        var opts = new MappingOptions()
            .AddConverter<string, int>(s => int.Parse(s ?? "0"));

        var dest = src.Adapt<SimpleDest>(opts)!;
        dest.Age.ShouldBe(15);
    }

    [Fact]
    public void ErrorHandler_Called_When_Converter_Throws_And_Strict_False()
    {
        var called = 0;
        var src = new SimpleSrc { Id = 1, Age = "bad" };
        var opts = new MappingOptions()
        {
            ErrorHandler = _ => called++,
            StrictMode = false
        }
        .AddConverter<string, int>(_ => throw new InvalidOperationException("boom"));

        var dest = src.Adapt<SimpleDest>(opts);

        called.ShouldBeGreaterThan(0);
        // Age falls back to default(int)=0 because converter failed and mapping continued
        dest!.Age.ShouldBe(0);
    }

    [Fact]
    public void Adapt_Handles_Cycles_Without_StackOverflow()
    {
        var a = new Node { Name = "a" };
        var b = new Node { Name = "b" };
        a.Next = b;
        b.Next = a; // cycle

        var mapped = a.Adapt<Node>();

        mapped.ShouldNotBeNull();
        mapped!.Name.ShouldBe("a");
        mapped.Next.ShouldNotBeNull();
        mapped.Next!.Name.ShouldBe("b");
        // Ensure the cycle is preserved on destination graph level
        mapped.Next.Next.ShouldBe(mapped);
    }

    [Fact]
    public void Null_Source_Returns_Default()
    {
        SimpleSrc? src = null;
        var dest = src.Adapt<SimpleDest>();
        dest.ShouldBeNull();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void Bool_String_Parses_To_Bool(string input, bool expected)
    {
        var src = new BoolSrc { B = input };
        var dest = src.Adapt<BoolDest>();
        dest.ShouldNotBeNull();
        dest!.B.ShouldBe(expected);
    }

    [Fact]
    public void Guid_String_Parses_Valid_And_Fails_ByPolicy_When_Invalid()
    {
        var g = Guid.NewGuid();
        var ok = new GuidSrc { G = g.ToString() }.Adapt<GuidDest>();
        ok.ShouldNotBeNull();
        ok!.G.ShouldBe(g);

        var badSrc = new GuidSrc { G = "not-a-guid" };
        // Default policy SetNullOrDefault -> default(Guid) == Guid.Empty
        var d1 = badSrc.Adapt<GuidDest>();
        d1.ShouldNotBeNull();
        d1!.G.ShouldBe(Guid.Empty);

        var optsSkip = new MappingOptions { ConversionFailure = ConversionFailureBehavior.Skip };
        var d2 = new GuidDest { G = g };
        badSrc.AdaptInto(d2, optsSkip);
        d2.G.ShouldBe(g); // kept existing

        var optsThrow = new MappingOptions { ConversionFailure = ConversionFailureBehavior.Throw };
        Should.Throw<InvalidCastException>(() => badSrc.Adapt<GuidDest>(optsThrow));
    }

    [Fact]
    public void DateTimeOffset_String_Parses_Common()
    {
        var iso = new DateTimeOffset(2025, 8, 16, 9, 10, 11, TimeSpan.Zero).ToString("O");
        var src = new { When = iso };
        var dest = src.Adapt(new { When = default(DateTimeOffset) }.GetType());
        dest.ShouldNotBeNull();
        var whenProp = dest!.GetType().GetProperty("When")!;
        ((DateTimeOffset)whenProp.GetValue(dest)!).Year.ShouldBe(2025);
    }

    [Fact]
    public void TimeSpan_String_Parses()
    {
        var span = TimeSpan.FromHours(1) + TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(3);
        var src = new { T = span.ToString() }; // c format
        var dest = src.Adapt(new { T = default(TimeSpan) }.GetType());
        dest.ShouldNotBeNull();
        var tProp = dest!.GetType().GetProperty("T")!;
        ((TimeSpan)tProp.GetValue(dest)!).ShouldBe(span);
    }

    [Fact]
    public void String_Destination_From_Primitive_Types()
    {
        var src = new DtoSrc { N = 123, T = TimeSpan.FromSeconds(5) };
        var dest = src.Adapt<DtoDest>();
        dest.ShouldNotBeNull();
        dest!.N.ShouldBe("123");
        dest.T.ShouldNotBeNull();
    }

    [Fact]
    public void AdaptInto_List_Clears_And_Fills_Existing_List()
    {
        var src = new NestedSrc
        {
            Items = new List<SimpleSrc>
            {
                new SimpleSrc { Id = 1, Name = "A", Age = "10" },
                new SimpleSrc { Id = 2, Name = "B", Age = "11" },
            }
        };

        var dest = new NestedDest { Items = new List<SimpleDest> { new SimpleDest { Id = 999, Age = 999 } } };

        src.AdaptInto(dest);

        dest.Items.ShouldNotBeNull();
        dest.Items!.Count.ShouldBe(2);
        dest.Items[0].Id.ShouldBe(1);
        dest.Items[1].Age.ShouldBe(11);
    }

    [Fact]
    public void StrictMode_Rethrows_On_Error()
    {
        var src = new SimpleSrc { Age = "bad" };
        var opts = new MappingOptions { StrictMode = true }
            .AddConverter<string, int>(_ => throw new InvalidOperationException("boom"));

        Should.Throw<InvalidOperationException>(() => src.Adapt<SimpleDest>(opts));
    }

    [Fact]
    public void Custom_Converter_Uses_BaseType_For_Derived_Source()
    {
        var dog = new Dog { Name = "Rex", Age = 3 };
        var opts = new MappingOptions()
            .AddConverter<Animal, string>(a => $"Animal:{a?.Name}");

        var dest = new { Name = (string?)null };
        var mapped = dog.Adapt(dest.GetType(), opts);
        mapped.ShouldNotBeNull();
        var nameProp = mapped!.GetType().GetProperty("Name")!;
        nameProp.GetValue(mapped).ShouldBe("Animal:Rex");
    }

    [Fact]
    public void Null_Source_To_ValueType_Returns_Default()
    {
        object? src = null;
        var val = src.Adapt<int>();
        val.ShouldBe(0);
    }

    // -------- Parse failure policies (Default/Skip/Throw) --------
    private sealed class DateOffsetSrc { public string? When { get; set; } }
    private sealed class DateOffsetDest { public DateTimeOffset When { get; set; } }
    private sealed class SpanSrc { public string? T { get; set; } }
    private sealed class SpanDest { public TimeSpan T { get; set; } }

    [Fact]
    public void DateTime_ParseFailure_Default_Sets_DefaultValue()
    {
        var src = new DateSrc { When = "not-a-date" };
        var dest = src.Adapt<DateDest>();
        dest.ShouldNotBeNull();
        dest!.When.ShouldBe(default);
    }

    [Fact]
    public void DateTime_ParseFailure_Skip_Keeps_Existing()
    {
        var src = new DateSrc { When = "bad" };
        var existing = new DateDest { When = new DateTime(2000, 1, 1) };
        var opts = new MappingOptions { ConversionFailure = ConversionFailureBehavior.Skip };
        src.AdaptInto(existing, opts);
        existing.When.ShouldBe(new DateTime(2000, 1, 1));
    }

    [Fact]
    public void DateTime_ParseFailure_Throw_Raises()
    {
        var src = new DateSrc { When = "bad" };
        var opts = new MappingOptions { ConversionFailure = ConversionFailureBehavior.Throw };
        Should.Throw<InvalidCastException>(() => src.Adapt<DateDest>(opts));
    }

    [Fact]
    public void DateTimeOffset_ParseFailure_Default_Sets_Default()
    {
        var src = new DateOffsetSrc { When = "x" };
        var dest = src.Adapt<DateOffsetDest>();
        dest.ShouldNotBeNull();
        dest!.When.ShouldBe(default);
    }

    [Fact]
    public void DateTimeOffset_ParseFailure_Skip_Keeps_Existing()
    {
        var src = new DateOffsetSrc { When = "x" };
        var existing = new DateOffsetDest { When = new DateTimeOffset(1999, 12, 31, 0, 0, 0, TimeSpan.Zero) };
        var opts = new MappingOptions { ConversionFailure = ConversionFailureBehavior.Skip };
        src.AdaptInto(existing, opts);
        existing.When.ShouldBe(new DateTimeOffset(1999, 12, 31, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void DateTimeOffset_ParseFailure_Throw_Raises()
    {
        var src = new DateOffsetSrc { When = "x" };
        var opts = new MappingOptions { ConversionFailure = ConversionFailureBehavior.Throw };
        Should.Throw<InvalidCastException>(() => src.Adapt<DateOffsetDest>(opts));
    }

    [Fact]
    public void TimeSpan_ParseFailure_Default_Sets_Default()
    {
        var src = new SpanSrc { T = "x" };
        var dest = src.Adapt<SpanDest>();
        dest.ShouldNotBeNull();
        dest!.T.ShouldBe(default);
    }

    [Fact]
    public void TimeSpan_ParseFailure_Skip_Keeps_Existing()
    {
        var src = new SpanSrc { T = "x" };
        var existing = new SpanDest { T = TimeSpan.FromSeconds(42) };
        var opts = new MappingOptions { ConversionFailure = ConversionFailureBehavior.Skip };
        src.AdaptInto(existing, opts);
        existing.T.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Fact]
    public void TimeSpan_ParseFailure_Throw_Raises()
    {
        var src = new SpanSrc { T = "x" };
        var opts = new MappingOptions { ConversionFailure = ConversionFailureBehavior.Throw };
        Should.Throw<InvalidCastException>(() => src.Adapt<SpanDest>(opts));
    }

    // -------- CaseSensitive across multiple properties --------
    private sealed class MultiCaseSrc { public string? firstName { get; set; } public string? lastName { get; set; } }
    private sealed class MultiCaseDest { public string? FirstName { get; set; } public string? LastName { get; set; } }

    [Fact]
    public void Adapt_CaseSensitive_True_Multiple_Properties_Are_Not_Mapped()
    {
        var src = new MultiCaseSrc { firstName = "Ann", lastName = "Lee" };
        var opts = new MappingOptions { CaseSensitive = true };
        var dest = src.Adapt<MultiCaseDest>(opts);
        dest.ShouldNotBeNull();
        dest!.FirstName.ShouldBeNull();
        dest.LastName.ShouldBeNull();
    }

    // -------- Arrays <-> List and existing destination array behavior --------
    private sealed class ArrayHolderSrc { public SimpleSrc[]? Items { get; set; } }
    private sealed class ListHolderDest { public List<SimpleDest>? Items { get; set; } }
    private sealed class ListHolderSrc { public List<SimpleSrc>? Items { get; set; } }
    private sealed class ArrayHolderDest { public SimpleDest[]? Items { get; set; } }

    [Fact]
    public void Map_SourceArray_To_DestinationList()
    {
        var src = new ArrayHolderSrc
        {
            Items = new[]
            {
                new SimpleSrc { Id = 1, Age = "10" },
                new SimpleSrc { Id = 2, Age = "11" }
            }
        };

        var dest = src.Adapt<ListHolderDest>();
        dest.ShouldNotBeNull();
        dest!.Items.ShouldNotBeNull();
        dest.Items!.Count.ShouldBe(2);
        dest.Items[1].Age.ShouldBe(11);
    }

    [Fact]
    public void Map_SourceList_To_DestinationArray()
    {
        var src = new ListHolderSrc
        {
            Items = new List<SimpleSrc>
            {
                new SimpleSrc { Id = 5, Age = "20" },
                new SimpleSrc { Id = 6, Age = "21" }
            }
        };

        var dest = src.Adapt<ArrayHolderDest>();
        dest.ShouldNotBeNull();
        dest!.Items.ShouldNotBeNull();
        dest.Items!.Length.ShouldBe(2);
        dest.Items[0].Age.ShouldBe(20);
        dest.Items[1].Id.ShouldBe(6);
    }

    [Fact]
    public void AdaptInto_With_Existing_Array_Creates_New_Instance_Not_InPlace()
    {
        var src = new ArrayHolderSrc
        {
            Items = new[]
            {
                new SimpleSrc { Id = 10, Age = "30" },
                new SimpleSrc { Id = 11, Age = "31" }
            }
        };

        var existingArray = new[] { new SimpleDest { Id = 999, Age = 999 } };
        var dest = new ArrayHolderDest { Items = existingArray };

        src.AdaptInto(dest);

        dest.Items.ShouldNotBeNull();
        // array instance should be replaced, not mutated in-place
        ReferenceEquals(dest.Items, existingArray).ShouldBeFalse();
        dest.Items!.Length.ShouldBe(2);
        dest.Items[0].Id.ShouldBe(10);
        dest.Items[1].Age.ShouldBe(31);
    }
}
