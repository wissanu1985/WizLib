using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Wiz.Utility.Text;

namespace Wiz.Utility.Extensions;

public static class StringExtensions
{
    // Null/Empty helpers
    public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);

    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);

    public static string? NullIfEmpty(this string? value) => string.IsNullOrEmpty(value) ? null : value;

    public static string DefaultIfNullOrEmpty(this string? value, string defaultValue)
        => string.IsNullOrEmpty(value) ? defaultValue : value!;

    // Safe substring (no exceptions, returns empty when out of range)
    public static string SafeSubstring(this string? value, int startIndex, int length)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (startIndex < 0) startIndex = 0;
        if (startIndex >= value.Length) return string.Empty;
        if (length < 0) length = 0;
        var maxLen = Math.Min(length, value.Length - startIndex);
        return maxLen <= 0 ? string.Empty : value.Substring(startIndex, maxLen);
    }

    // Truncate with optional ellipsis
    public static string? Truncate(this string? value, int maxLength, string? ellipsis = null)
    {
        if (value is null) return null;
        if (maxLength < 0) throw new ArgumentOutOfRangeException(nameof(maxLength));
        if (value.Length <= maxLength) return value;
        if (!string.IsNullOrEmpty(ellipsis) && ellipsis!.Length < maxLength)
            return value.Substring(0, maxLength - ellipsis.Length) + ellipsis;
        return value.Substring(0, maxLength);
    }

    // Normalize internal whitespace to single spaces and trim ends
    public static string NormalizeWhitespace(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var result = Regex.Replace(value, "\\s+", " ", RegexOptions.CultureInvariant);
        return result.Trim();
    }

    // Remove diacritics (accents) using Unicode normalization
    public static string RemoveDiacritics(this string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // TitleCase with culture awareness; lowers first to handle ALL-CAPS words
    public static string ToTitleCase(this string? value, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        culture ??= CultureInfo.CurrentCulture;
        var ti = culture.TextInfo;
        return ti.ToTitleCase(value.ToLower(culture));
    }

    // Humanize: split PascalCase/camelCase/underscores/dashes to readable sentence
    public static string Humanize(this string? value, bool titleCase = false, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        culture ??= CultureInfo.CurrentCulture;
        var s = value!;
        // Replace separators with space
        s = s.Replace('_', ' ').Replace('-', ' ');
        // Insert spaces between camel/Pascal boundaries
        s = Regex.Replace(s, "([a-z0-9])([A-Z])", "$1 $2", RegexOptions.CultureInvariant);
        // Collapse whitespace
        s = Regex.Replace(s, "\\s+", " ", RegexOptions.CultureInvariant).Trim();
        if (titleCase)
        {
            return culture.TextInfo.ToTitleCase(s.ToLower(culture));
        }
        // Sentence case: lowercase all then capitalize first character
        var lower = s.ToLower(culture);
        return lower.Length == 0 ? lower : char.ToUpper(lower[0], culture) + lower.Substring(1);
    }

    // Dehumanize to PascalCase: remove separators and capitalize each word
    public static string DehumanizeToPascalCase(this string? value, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        culture ??= CultureInfo.CurrentCulture;
        var parts = Regex.Split(value!.Trim(), "[^A-Za-z0-9]+");
        var sb = new StringBuilder();
        foreach (var p in parts)
        {
            if (string.IsNullOrEmpty(p)) continue;
            var lower = p.ToLower(culture);
            sb.Append(char.ToUpper(lower[0], culture));
            if (lower.Length > 1) sb.Append(lower.Substring(1));
        }
        return sb.ToString();
    }

    // Case-insensitive operations
    public static bool EqualsOrdinalIgnoreCase(this string? value, string? other)
        => string.Equals(value, other, StringComparison.OrdinalIgnoreCase);

    public static bool ContainsOrdinalIgnoreCase(this string? value, string? part)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(part)) return false;
        return value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // Base64 encode/decode helpers
    public static string ToBase64(this string? value, Encoding? encoding = null)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        encoding ??= Encoding.UTF8;
        return Convert.ToBase64String(encoding.GetBytes(value));
    }

    public static string FromBase64(this string? value, Encoding? encoding = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        encoding ??= Encoding.UTF8;
        try
        {
            var bytes = Convert.FromBase64String(value.Trim());
            return encoding.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    // Strip basic HTML tags using a simple regex (for complex cases, use an HTML parser)
    public static string StripHtml(this string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var withoutTags = Regex.Replace(value, "<[^>]+>", string.Empty, RegexOptions.CultureInvariant);
        return Regex.Replace(withoutTags, "\\s+", " ", RegexOptions.CultureInvariant).Trim();
    }

    // Slugify overload with transliterator to support cross-script conversion
    public static string Slugify(this string? value, IStringTransliterator? transliterator, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var src = value!;
        if (transliterator is not null)
        {
            src = transliterator.Transliterate(src);
        }
        var text = src.RemoveDiacritics().ToLowerInvariant();
        text = Regex.Replace(text, "[^a-z0-9]+", "-", RegexOptions.CultureInvariant);
        text = Regex.Replace(text, "-+", "-", RegexOptions.CultureInvariant);
        text = text.Trim('-');
        if (maxLength > 0 && text.Length > maxLength)
            text = text.Substring(0, maxLength).Trim('-');
        return text;
    }

    // Slugify for URLs: lowercase, hyphens, ASCII only, collapse duplicates, trim
    public static string Slugify(this string? value, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var text = value.RemoveDiacritics().ToLowerInvariant();
        text = Regex.Replace(text, "[^a-z0-9]+", "-", RegexOptions.CultureInvariant);
        text = Regex.Replace(text, "-+", "-", RegexOptions.CultureInvariant);
        text = text.Trim('-');
        if (maxLength > 0 && text.Length > maxLength)
            text = text.Substring(0, maxLength).Trim('-');
        return text;
    }

    // SnakeCase: words separated by underscore
    public static string ToSnakeCase(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var text = value.RemoveDiacritics();
        text = Regex.Replace(text, "([a-z0-9])([A-Z])", "$1_$2", RegexOptions.CultureInvariant);
        text = Regex.Replace(text, "[^A-Za-z0-9]+", "_", RegexOptions.CultureInvariant);
        text = Regex.Replace(text, "_+", "_", RegexOptions.CultureInvariant);
        return text.Trim('_').ToLowerInvariant();
    }

    // KebabCase: words separated by hyphen (similar to slug)
    public static string ToKebabCase(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var text = value.RemoveDiacritics();
        text = Regex.Replace(text, "([a-z0-9])([A-Z])", "$1-$2", RegexOptions.CultureInvariant);
        text = Regex.Replace(text, "[^A-Za-z0-9]+", "-", RegexOptions.CultureInvariant);
        text = Regex.Replace(text, "-+", "-", RegexOptions.CultureInvariant);
        return text.Trim('-').ToLowerInvariant();
    }

    // Basic Levenshtein wrappers (use DefaultStringSimilarity). For advanced, plug an external impl via DI.
    public static int LevenshteinDistance(this string? a, string? b)
    {
        var algo = new DefaultStringSimilarity();
        return algo.LevenshteinDistance(a ?? string.Empty, b ?? string.Empty);
    }

    public static double SimilarityRatio(this string? a, string? b)
    {
        var algo = new DefaultStringSimilarity();
        return algo.Similarity(a ?? string.Empty, b ?? string.Empty);
    }

    // Convert any Unicode decimal digits to ASCII '0'-'9' (e.g., Thai ๐-๙)
    public static string NormalizeDigits(this string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsDigit(ch))
            {
                int d = CharUnicodeInfo.GetDigitValue(ch);
                if (d >= 0 && d <= 9)
                {
                    sb.Append((char)('0' + d));
                    continue;
                }
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    // Number to words (Thai/English) for Thai Baht currency
    public static string ToThaiBahtText(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (!decimal.TryParse(value.NormalizeDigits(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return string.Empty;
        return amount.ToThaiBahtText();
    }

    public static string ToThaiBahtText(this decimal amount)
    {
        var negative = amount < 0;
        amount = Math.Abs(amount);
        var baht = (long)Math.Floor(amount);
        var satang = (int)Math.Round((amount - baht) * 100m, MidpointRounding.AwayFromZero);
        if (satang == 100) { baht += 1; satang = 0; }

        var parts = new List<string>();
        if (baht == 0 && satang > 0)
        {
            parts.Add(ThaiNumberToWords(satang));
            parts.Add("สตางค์");
        }
        else
        {
            parts.Add(baht == 0 ? "ศูนย์" : ThaiNumberToWords(baht));
            parts.Add("บาท");
            if (satang == 0)
            {
                parts.Add("ถ้วน");
            }
            else
            {
                parts.Add(ThaiNumberToWords(satang));
                parts.Add("สตางค์");
            }
        }
        var text = string.Join(string.Empty, parts);
        return negative ? "ลบ" + text : text;
    }

    public static string ToEnglishBahtText(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (!decimal.TryParse(value.NormalizeDigits(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return string.Empty;
        return amount.ToEnglishBahtText();
    }

    public static string ToEnglishBahtText(this decimal amount)
    {
        var negative = amount < 0;
        amount = Math.Abs(amount);
        var baht = (long)Math.Floor(amount);
        var satang = (int)Math.Round((amount - baht) * 100m, MidpointRounding.AwayFromZero);
        if (satang == 100) { baht += 1; satang = 0; }

        var sb = new StringBuilder();
        if (baht == 0 && satang > 0)
        {
            sb.Append(EnglishNumberToWords(satang)).Append(" satang");
        }
        else
        {
            sb.Append(baht == 0 ? "zero" : EnglishNumberToWords(baht)).Append(" baht");
            if (satang == 0)
            {
                sb.Append(" only");
            }
            else
            {
                sb.Append(" and ").Append(EnglishNumberToWords(satang)).Append(" satang");
            }
        }
        var text = sb.ToString();
        return negative ? "minus " + text : text;
    }

    // Helpers: Thai number to words (supports groups of 6 digits with repeated "ล้าน")
    private static string ThaiNumberToWords(long number)
    {
        if (number == 0) return "ศูนย์";
        var chunks = new List<string>();
        int groupIndex = 0;
        while (number > 0)
        {
            int group = (int)(number % 1_000_000);
            if (group > 0)
            {
                var part = ThaiReadSixDigits(group);
                if (groupIndex > 0)
                {
                    part += new string(' ', 0) + "ล้าน";
                    if (groupIndex > 1)
                    {
                        // repeat ล้าน for each additional group
                        for (int i = 1; i < groupIndex; i++) part += "ล้าน";
                    }
                }
                chunks.Insert(0, part);
            }
            number /= 1_000_000;
            groupIndex++;
        }
        return string.Join(string.Empty, chunks);
    }

    private static string ThaiReadSixDigits(int number)
    {
        if (number == 0) return string.Empty;
        string[] digits = { "ศูนย์", "หนึ่ง", "สอง", "สาม", "สี่", "ห้า", "หก", "เจ็ด", "แปด", "เก้า" };
        string[] units = { "", "สิบ", "ร้อย", "พัน", "หมื่น", "แสน" };
        var sb = new StringBuilder();
        int[] arr = new int[6];
        for (int i = 0; i < 6; i++) { arr[i] = number % 10; number /= 10; }
        for (int pos = 5; pos >= 0; pos--)
        {
            int d = arr[pos];
            if (d == 0) continue;
            if (pos == 1) // tens
            {
                if (d == 1) { sb.Append("สิบ"); }
                else if (d == 2) { sb.Append("ยี่สิบ"); }
                else { sb.Append(digits[d]).Append("สิบ"); }
            }
            else if (pos == 0) // ones
            {
                if (d == 1 && (arr[1] > 0 || arr[2] > 0 || arr[3] > 0 || arr[4] > 0 || arr[5] > 0))
                    sb.Append("เอ็ด");
                else
                    sb.Append(digits[d]);
            }
            else
            {
                sb.Append(digits[d]).Append(units[pos]);
            }
        }
        return sb.ToString();
    }

    // Helpers: English number to words up to trillions
    private static string EnglishNumberToWords(long number)
    {
        if (number == 0) return "zero";
        if (number < 0) return "minus " + EnglishNumberToWords(Math.Abs(number));

        string[] unitsMap = { "zero","one","two","three","four","five","six","seven","eight","nine","ten","eleven","twelve","thirteen","fourteen","fifteen","sixteen","seventeen","eighteen","nineteen" };
        string[] tensMap  = { "zero","ten","twenty","thirty","forty","fifty","sixty","seventy","eighty","ninety" };

        var parts = new List<string>();

        void AppendChunk(long n, string scale)
        {
            if (n == 0) return;
            var chunk = new StringBuilder();
            long hundreds = n / 100;
            long remainder = n % 100;
            if (hundreds > 0)
            {
                chunk.Append(unitsMap[hundreds]).Append(" hundred");
                if (remainder > 0) chunk.Append(" ");
            }
            if (remainder > 0)
            {
                if (remainder < 20)
                {
                    chunk.Append(unitsMap[remainder]);
                }
                else
                {
                    long tens = remainder / 10;
                    long ones = remainder % 10;
                    chunk.Append(tensMap[tens]);
                    if (ones > 0) chunk.Append("-").Append(unitsMap[ones]);
                }
            }
            if (!string.IsNullOrEmpty(scale)) chunk.Append(" ").Append(scale);
            parts.Add(chunk.ToString());
        }

        long trillion = number / 1_000_000_000_000;
        number %= 1_000_000_000_000;
        long billion = number / 1_000_000_000;
        number %= 1_000_000_000;
        long million = number / 1_000_000;
        number %= 1_000_000;
        long thousand = number / 1_000;
        long rest = number % 1_000;

        AppendChunk(trillion, "trillion");
        AppendChunk(billion, "billion");
        AppendChunk(million, "million");
        AppendChunk(thousand, "thousand");
        AppendChunk(rest, string.Empty);

        return string.Join(" ", parts);
    }

    // Validate Thai Citizen ID (13 digits) using checksum
    // Algorithm: sum(d[i] * (13 - i)) for i=0..11, check = (11 - (sum % 11)) % 10 == d[12]
    public static bool IsThaiCitizenId(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        // Normalize digits (handle Thai numerals) and keep only ASCII digits
        var normalized = value.NormalizeDigits();
        var digits = new StringBuilder(13);
        foreach (var ch in normalized)
        {
            if (ch >= '0' && ch <= '9') digits.Append(ch);
        }
        if (digits.Length != 13) return false;

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int d = digits[i] - '0';
            int weight = 13 - i; // 13..2
            sum += d * weight;
        }
        int check = (11 - (sum % 11)) % 10;
        int last = digits[12] - '0';
        return check == last;
    }

    // SHA-256 hash (hex lowercase) of UTF-8 bytes
    public static string ToSha256Hex(this string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }
}
