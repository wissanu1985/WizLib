using NSubstitute;
using Shouldly;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Linq;
using Wiz.Utility.Extensions;
using Wiz.Utility.Text;
using Xunit;

namespace Wiz.Utility.Test.Extensions
{
    public class StringExtensionsTests
    {
        // Null/Empty helpers
        [Fact]
        public void IsNullOrEmpty_Null_ReturnsTrue()
        {
            string? s = null;
            s.IsNullOrEmpty().ShouldBeTrue();
            "".IsNullOrEmpty().ShouldBeTrue();
            "text".IsNullOrEmpty().ShouldBeFalse();
        }

        [Fact]
        public void IsNullOrWhiteSpace_VariousInputs_ReturnsExpected()
        {
            ((string?)null).IsNullOrWhiteSpace().ShouldBeTrue();
            "".IsNullOrWhiteSpace().ShouldBeTrue();
            "  \t\n".IsNullOrWhiteSpace().ShouldBeTrue();
            " a ".IsNullOrWhiteSpace().ShouldBeFalse();
        }

        [Fact]
        public void NullIfEmpty_EmptyOrNull_ReturnsNull_OtherwiseSame()
        {
            ((string?)null).NullIfEmpty().ShouldBeNull();
            "".NullIfEmpty().ShouldBeNull();
            "abc".NullIfEmpty().ShouldBe("abc");
        }

        [Fact]
        public void DefaultIfNullOrEmpty_WhenNullOrEmpty_ReturnsDefault()
        {
            ((string?)null).DefaultIfNullOrEmpty("def").ShouldBe("def");
            "".DefaultIfNullOrEmpty("def").ShouldBe("def");
            "abc".DefaultIfNullOrEmpty("def").ShouldBe("abc");
        }

        // SafeSubstring
        [Theory]
        [InlineData(null, 0, 3, "")]
        [InlineData("hello", -5, 2, "he")]
        [InlineData("hello", 10, 2, "")]
        [InlineData("hello", 1, -1, "")]
        [InlineData("hello", 1, 2, "el")]
        public void SafeSubstring_Inputs_ReturnsExpected(string? s, int start, int len, string expected)
            => s.SafeSubstring(start, len).ShouldBe(expected);

        // Truncate
        [Fact]
        public void Truncate_Null_ReturnsNull()
        {
            string? s = null;
            s.Truncate(5).ShouldBeNull();
        }

        [Fact]
        public void Truncate_NegativeMax_Throws()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => "abc".Truncate(-1));
        }

        [Fact]
        public void Truncate_WithinLimit_ReturnsOriginal()
        {
            "hello".Truncate(10).ShouldBe("hello");
        }

        [Fact]
        public void Truncate_WithEllipsis_AppendsWhenFits()
        {
            "helloworld".Truncate(7, "...").ShouldBe("hell...");
        }

        [Fact]
        public void Truncate_EllipsisTooLong_IgnoresEllipsis()
        {
            "helloworld".Truncate(5, "[ellipsis]").ShouldBe("hello");
        }

        // Whitespace/diacritics/title/humanize/dehumanize
        [Fact]
        public void NormalizeWhitespace_MixedSpaces_CollapsesAndTrims()
        {
            "  a\t  b\n c  ".NormalizeWhitespace().ShouldBe("a b c");
            ((string?)null).NormalizeWhitespace().ShouldBe("");
        }

        [Fact]
        public void RemoveDiacritics_CommonAccents_Stripped()
        {
            "Café déjà vu – ångström".RemoveDiacritics().ShouldBe("Cafe deja vu – angstrom");
            ((string?)null).RemoveDiacritics().ShouldBe("");
        }

        [Fact]
        public void ToTitleCase_EnUs_ProducesTitleCase()
        {
            var en = new CultureInfo("en-US");
            "hELLo woRLD".ToTitleCase(en).ShouldBe("Hello World");
            ((string?)null).ToTitleCase(en).ShouldBe("");
        }

        [Fact]
        public void Humanize_MixedDelimiters_SentenceOrTitleCase()
        {
            var en = new CultureInfo("en-US");
            var input = "HelloWorldXML_test-case";
            input.Humanize(false, en).ShouldBe("Hello world xml test case");
            input.Humanize(true, en).ShouldBe("Hello World Xml Test Case");
            ((string?)null).Humanize().ShouldBe("");
        }

        [Fact]
        public void DehumanizeToPascalCase_MixedDelimiters_CombinedPascalCase()
        {
            var en = new CultureInfo("en-US");
            "hello world-XML_case".DehumanizeToPascalCase(en).ShouldBe("HelloWorldXmlCase");
            ((string?)null).DehumanizeToPascalCase(en).ShouldBe("");
        }

        // Case-insensitive helpers
        [Fact]
        public void EqualsOrdinalIgnoreCase_Basic()
        {
            "AbC".EqualsOrdinalIgnoreCase("aBc").ShouldBeTrue();
            "abc".EqualsOrdinalIgnoreCase("abc ").ShouldBeFalse();
        }

        [Fact]
        public void ContainsOrdinalIgnoreCase_Basic()
        {
            "hello world".ContainsOrdinalIgnoreCase("WORLD").ShouldBeTrue();
            "hello".ContainsOrdinalIgnoreCase("").ShouldBeFalse();
            ((string?)null).ContainsOrdinalIgnoreCase("a").ShouldBeFalse();
        }

        // Base64
        [Fact]
        public void ToBase64_And_FromBase64_Roundtrip()
        {
            var original = "Hello, 世界";
            var b64 = original.ToBase64();
            b64.ShouldBe(Convert.ToBase64String(Encoding.UTF8.GetBytes(original)));
            var decoded = b64.FromBase64();
            decoded.ShouldBe(original);
        }

        [Fact]
        public void ToBase64_NullOrEmpty_ReturnsEmpty()
        {
            ((string?)null).ToBase64().ShouldBe("");
            "".ToBase64().ShouldBe("");
        }

        [Fact]
        public void FromBase64_InvalidInput_ReturnsEmpty()
        {
            "###".FromBase64().ShouldBe("");
            ((string?)null).FromBase64().ShouldBe("");
        }

        // StripHtml
        [Fact]
        public void StripHtml_SimpleTags_RemovedAndCollapsed()
        {
            var html = "<p>Hello<br>   world</p>";
            html.StripHtml().ShouldBe("Hello world");
            ((string?)null).StripHtml().ShouldBe("");
        }

        // Slugify
        [Fact]
        public void Slugify_Basic_RemovesDiacriticsAndSeparators()
        {
            "Café déjà vu!".Slugify().ShouldBe("cafe-deja-vu");
        }

        [Fact]
        public void Slugify_WithTransliterator_UsesTransliteration()
        {
            var t = Substitute.For<IStringTransliterator>();
            t.Transliterate(Arg.Any<string>()).Returns(ci => "sawasdee thai123");
            var result = "สวัสดี ไทย123!!".Slugify(t, 200);
            result.ShouldBe("sawasdee-thai123");
        }

        [Fact]
        public void Slugify_MaxLength_TrimsAndCleansEdges()
        {
            "Hello world".Slugify(5).ShouldBe("hello");
        }

        // Snake/Kebab
        [Fact]
        public void ToSnakeCase_Mixed_ReturnsExpected()
        {
            "HelloWorld test-Case".ToSnakeCase().ShouldBe("hello_world_test_case");
            ((string?)null).ToSnakeCase().ShouldBe("");
        }

        [Fact]
        public void ToKebabCase_Mixed_ReturnsExpected()
        {
            "HelloWorld test_Case".ToKebabCase().ShouldBe("hello-world-test-case");
            ((string?)null).ToKebabCase().ShouldBe("");
        }

        // Similarity/Distance
        [Fact]
        public void LevenshteinDistance_KnownExample_Returns3()
        {
            "kitten".LevenshteinDistance("sitting").ShouldBe(3);
            "".LevenshteinDistance("abc").ShouldBe(3);
            ((string?)null).LevenshteinDistance(null).ShouldBe(0);
        }

        [Fact]
        public void SimilarityRatio_KnownExample_Approx()
        {
            var ratio = "kitten".SimilarityRatio("sitting"); // 1 - 3/7 ≈ 0.5714285
            ratio.ShouldBeInRange(0.57142, 0.57144);
            "same".SimilarityRatio("same").ShouldBe(1.0);
            "".SimilarityRatio("a").ShouldBe(0.0);
        }

        // Digits normalization
        [Fact]
        public void NormalizeDigits_ThaiNumerals_ToAscii()
        {
            var input = "ราคา ๑๒๓.๔๕ บาท";
            input.NormalizeDigits().ShouldBe("ราคา 123.45 บาท");
        }

        // Baht text (Thai)
        [Fact]
        public void ToThaiBahtText_FromString_ValidThaiDigits()
        {
            "๑๒๓.๔๕".ToThaiBahtText().ShouldBe("หนึ่งร้อยยี่สิบสามบาทสี่สิบห้าสตางค์");
            "abc".ToThaiBahtText().ShouldBe("");
            ((string?)null).ToThaiBahtText().ShouldBe("");
        }

        [Fact]
        public void ToThaiBahtText_FromDecimal_CommonCases()
        {
            0m.ToThaiBahtText().ShouldBe("ศูนย์บาทถ้วน");
            (-1m).ToThaiBahtText().ShouldBe("ลบหนึ่งบาทถ้วน");
            0.25m.ToThaiBahtText().ShouldBe("ยี่สิบห้าสตางค์");
            1.995m.ToThaiBahtText().ShouldBe("สองบาทถ้วน"); // rounding satang to 100
        }

        // Baht text (English)
        [Fact]
        public void ToEnglishBahtText_FromString_AndDecimal()
        {
            "123.45".ToEnglishBahtText().ShouldBe("one hundred twenty-three baht and forty-five satang");
            0m.ToEnglishBahtText().ShouldBe("zero baht only");
            1.25m.ToEnglishBahtText().ShouldBe("one baht and twenty-five satang");
            (-12m).ToEnglishBahtText().ShouldBe("minus twelve baht only");
            1.995m.ToEnglishBahtText().ShouldBe("two baht only");
        }

        // Thai citizen ID
        [Fact]
        public void IsThaiCitizenId_ComputedValidAndInvalid_Cases()
        {
            string first12 = "110170020345"; // any 12 digits
            string valid = first12 + ComputeThaiIdCheckDigit(first12);
            valid.IsThaiCitizenId().ShouldBeTrue();

            // formatted and with Thai numerals should also pass
            var thaiDigits = string.Concat(valid.Select(ch => ch switch
            {
                '0' => '๐', '1' => '๑', '2' => '๒', '3' => '๓', '4' => '๔',
                '5' => '๕', '6' => '๖', '7' => '๗', '8' => '๘', '9' => '๙', _ => ch
            }));
            ($"{thaiDigits.Substring(0,1)}-{thaiDigits.Substring(1,4)}-{thaiDigits.Substring(5,5)}-{thaiDigits.Substring(10,2)}-{thaiDigits.Substring(12,1)}").IsThaiCitizenId().ShouldBeTrue();

            // invalid cases
            "123".IsThaiCitizenId().ShouldBeFalse();
            ((string?)null).IsThaiCitizenId().ShouldBeFalse();
        }

        private static char ComputeThaiIdCheckDigit(string first12)
        {
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int d = first12[i] - '0';
                int weight = 13 - i;
                sum += d * weight;
            }
            int check = (11 - (sum % 11)) % 10;
            return (char)('0' + check);
        }

        // SHA-256
        [Fact]
        public void ToSha256Hex_ABC_KnownVector()
        {
            var hex = "abc".ToSha256Hex();
            hex.ShouldBe("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
            ((string?)null).ToSha256Hex().ShouldBe("");
        }

        // Benchmark (skip by default)
        [Fact(Skip = "Performance smoke test; skip in CI")]
        [Trait("Category", "Benchmark")]
        public void Slugify_LongText_Benchmark()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 10_000; i++) sb.Append('a');
            var text = sb.ToString();
            var sw = Stopwatch.StartNew();
            var slug = text.Slugify();
            sw.Stop();
            slug.Length.ShouldBeGreaterThan(0);
            sw.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
        }
    }
}
