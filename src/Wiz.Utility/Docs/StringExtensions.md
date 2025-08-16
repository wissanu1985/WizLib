# StringExtensions

> ส่วนขยายสตริงที่ใช้งานได้จริง ครอบคลุมงานทั่วไป: null/empty checks, safe substring, whitespace/diacritics normalization, case/snake/kebab/slug, Base64, Thai/English currency text, Thai citizen ID validation, similarity (Levenshtein), hashing (SHA-256) ฯลฯ

Last updated: 2025-08-16

## Overview | ภาพรวม
- ชุดเมธอด extension ใต้เนมสเปซ `Wiz.Utility.Extensions` สำหรับ `string` และบางเมธอดสำหรับ `decimal` ที่เกี่ยวข้อง (เช่น `ToThaiBahtText(this decimal)`).
- ออกแบบให้ “ปลอดภัย” ต่อค่า null, ใช้งานง่าย และไม่มี dependency ภายนอก ยกเว้นส่วน transliteration ที่เปิดทางให้เสียบ implementation ภายนอกผ่าน `Wiz.Utility.Text.IStringTransliterator`.

## Audience | กลุ่มผู้ใช้
- นักพัฒนา .NET/C# ที่ต้องการ utility กับข้อความสายงานทั่วไป (แปลงรูปแบบ, สร้าง slug, เปรียบเทียบความคล้าย, ตรวจเลขบัตรประชาชนไทย ฯลฯ).

## Prerequisites | ข้อกำหนดเบื้องต้น
```csharp
using Wiz.Utility.Extensions;
using Wiz.Utility.Text; // สำหรับ IStringTransliterator, DefaultTransliterator
```

## Quickstart | ใช้งานรวดเร็ว
```csharp
using System;
using System.Globalization;
using Wiz.Utility.Extensions;
using Wiz.Utility.Text;

class Demo
{
    static void Main()
    {
        // Null/Empty helpers
        string? s = null;
        Console.WriteLine(s.IsNullOrWhiteSpace()); // True
        Console.WriteLine(s.DefaultIfNullOrEmpty("N/A")); // "N/A"

        // SafeSubstring / Truncate
        Console.WriteLine("Hello".SafeSubstring(10, 2)); // ""
        Console.WriteLine("Hello world".Truncate(5, "…")); // "Hell…"

        // Whitespace & Diacritics
        Console.WriteLine("  a\t b  c  ".NormalizeWhitespace()); // "a b c"
        Console.WriteLine("Crème Brûlée".RemoveDiacritics()); // "Creme Brulee"

        // Humanize / Dehumanize
        Console.WriteLine("CustomerID".Humanize()); // "Customer id"
        Console.WriteLine("customer id".DehumanizeToPascalCase()); // "CustomerId"

        // Case conversions
        Console.WriteLine("HelloWorld".ToSnakeCase()); // "hello_world"
        Console.WriteLine("HelloWorld".ToKebabCase()); // "hello-world"

        // Slugify (ASCII only). For non-Latin scripts, provide a real transliterator.
        Console.WriteLine("C# is great!".Slugify()); // "c-is-great"
        Console.WriteLine("เชียงใหม่ – ไทย".Slugify(new DefaultTransliterator()));
        // NOTE: DefaultTransliterator is no-op; to transliterate Thai -> ASCII, plug a real implementation.

        // Base64
        var b64 = "hello".ToBase64();
        Console.WriteLine(b64); // aGVsbG8=
        Console.WriteLine(b64.FromBase64()); // hello

        // Digits normalization (e.g., Thai, Arabic-Indic -> ASCII)
        Console.WriteLine("๑๒๓4٥".NormalizeDigits()); // "12345"

        // Currency text (Thai Baht)
        Console.WriteLine("1,234.56".ToThaiBahtText()); // "หนึ่งพันสองร้อยสามสิบสี่บาทห้าสิบหกสตางค์"
        Console.WriteLine(1234.56m.ToEnglishBahtText()); // "one thousand two hundred thirty-four baht and fifty-six satang"

        // Thai citizen ID validation
        Console.WriteLine("1101700203451".IsThaiCitizenId()); // True/False (ตามเลขจริง)

        // Similarity / Levenshtein
        Console.WriteLine("kitten".LevenshteinDistance("sitting")); // 3
        Console.WriteLine("kitten".SimilarityRatio("sitting")); // ~0.57

        // SHA-256 (hex)
        Console.WriteLine("hello".ToSha256Hex());
        // 2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824

        // Title case
        Console.WriteLine("hello WORLD".ToTitleCase(new CultureInfo("en-US"))); // "Hello World"
    }
}
```

## Unit-tested Examples | ตัวอย่างยืนยันด้วย Unit Tests
- __SafeSubstring__ (`SafeSubstring_Inputs_ReturnsExpected` ใน `tests/Wiz.Utility.Test/Extensions/StringExtensionsTests.cs`)
  ```csharp
  "hello".SafeSubstring(-5, 2); // "he"
  "hello".SafeSubstring(10, 2); // ""
  "hello".SafeSubstring(1, 2);  // "el"
  ```

- __Truncate__ (`Truncate_*` tests)
  ```csharp
  ((string?)null).Truncate(5);            // null
  "hello".Truncate(10);                  // "hello"
  "helloworld".Truncate(7, "...");     // "hell..."
  "helloworld".Truncate(5, "[ellipsis]"); // "hello"
  ```

- **Whitespace/Diacritics/Title/Humanize/Dehumanize**
  ```csharp
  "  a\t  b\n c  ".NormalizeWhitespace();                   // "a b c"
  "Café déjà vu – ångström".RemoveDiacritics();               // "Cafe deja vu – angstrom"
  "hELLo woRLD".ToTitleCase(new CultureInfo("en-US"));       // "Hello World"
  var input = "HelloWorldXML_test-case";
  input.Humanize(false, new CultureInfo("en-US"));            // "Hello world xml test case"
  input.Humanize(true, new CultureInfo("en-US"));             // "Hello World Xml Test Case"
  "hello world-XML_case".DehumanizeToPascalCase(new CultureInfo("en-US")); // "HelloWorldXmlCase"
  ```

- **Case-insensitive & Contains**
  ```csharp
  "AbC".EqualsOrdinalIgnoreCase("aBc");   // true
  "abc".EqualsOrdinalIgnoreCase("abc ");  // false
  "hello world".ContainsOrdinalIgnoreCase("WORLD"); // true
  ((string?)null).ContainsOrdinalIgnoreCase("a");    // false
  ```

- **Base64**
  ```csharp
  var original = "Hello, 世界";
  var b64 = original.ToBase64();  // matches Convert.ToBase64String(Encoding.UTF8.GetBytes(original))
  var decoded = b64.FromBase64(); // equals original
  "###".FromBase64();             // ""
  ```

- **Slugify**
  ```csharp
  "Café déjà vu!".Slugify(); // "cafe-deja-vu"
  var t = Substitute.For<Wiz.Utility.Text.IStringTransliterator>();
  t.Transliterate(Arg.Any<string>()).Returns(ci => "sawasdee thai123");
  "สวัสดี ไทย123!!".Slugify(t, 200); // "sawasdee-thai123"
  "Hello world".Slugify(5);          // "hello"
  ```

- **Snake/Kebab**
  ```csharp
  "HelloWorld test-Case".ToSnakeCase(); // "hello_world_test_case"
  "HelloWorld test_Case".ToKebabCase(); // "hello-world-test-case"
  ```

- **Similarity/Distance**
  ```csharp
  "kitten".LevenshteinDistance("sitting"); // 3
  "kitten".SimilarityRatio("sitting");     // ≈ 0.57143
  "same".SimilarityRatio("same");           // 1.0
  "".SimilarityRatio("a");                  // 0.0
  ```

- **Digits normalization (Thai -> ASCII)**
  ```csharp
  "ราคา ๑๒๓.๔๕ บาท".NormalizeDigits(); // "ราคา 123.45 บาท"
  ```

- **Thai/English Baht text**
  ```csharp
  "๑๒๓.๔๕".ToThaiBahtText(); // "หนึ่งร้อยยี่สิบสามบาทสี่สิบห้าสตางค์"
  0m.ToThaiBahtText();         // "ศูนย์บาทถ้วน"
  (-1m).ToThaiBahtText();      // "ลบหนึ่งบาทถ้วน"
  0.25m.ToThaiBahtText();      // "ยี่สิบห้าสตางค์"
  1.995m.ToThaiBahtText();     // "สองบาทถ้วน"

  "123.45".ToEnglishBahtText(); // "one hundred twenty-three baht and forty-five satang"
  0m.ToEnglishBahtText();        // "zero baht only"
  1.25m.ToEnglishBahtText();     // "one baht and twenty-five satang"
  (-12m).ToEnglishBahtText();    // "minus twelve baht only"
  1.995m.ToEnglishBahtText();    // "two baht only"
  ```

- **Thai Citizen ID validation**
  ```csharp
  // A computed valid 13-digit string should be true; malformed/short should be false.
  "123".IsThaiCitizenId();       // false
  ((string?)null).IsThaiCitizenId(); // false
  ```

- **SHA-256**
  ```csharp
  "abc".ToSha256Hex(); // "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"
  ((string?)null).ToSha256Hex(); // ""
  ```

## API Reference | สรุปเมธอดสำคัญ (ย่อ)
- Null/Empty
  - `bool IsNullOrEmpty(this string? value)`
  - `bool IsNullOrWhiteSpace(this string? value)`
  - `string? NullIfEmpty(this string? value)`
  - `string DefaultIfNullOrEmpty(this string? value, string defaultValue)`

- Substring & Trimming
  - `string SafeSubstring(this string? value, int startIndex, int length)`
    - ไม่ throw เมื่อ out-of-range; คืน `""` เมื่อเกินช่วง
  - `string? Truncate(this string? value, int maxLength, string? ellipsis = null)`

- Normalization
  - `string NormalizeWhitespace(this string? value)`
  - `string RemoveDiacritics(this string? value)`
  - `string NormalizeDigits(this string? value)`
  - `string StripHtml(this string? value)`

- Case/Text Format
  - `string ToTitleCase(this string? value, CultureInfo? culture = null)`
  - `string Humanize(this string? value, bool titleCase = false, CultureInfo? culture = null)`
  - `string DehumanizeToPascalCase(this string? value, CultureInfo? culture = null)`
  - `string ToSnakeCase(this string? value)`
  - `string ToKebabCase(this string? value)`

- Slug
  - `string Slugify(this string? value, int maxLength = 200)`
  - `string Slugify(this string? value, IStringTransliterator? transliterator, int maxLength = 200)`
    - หากมี non-Latin scripts ให้จัดเตรียม `IStringTransliterator` ที่แปลงเป็น ASCII จริง ๆ (เช่นใช้ไลบรารีภายนอก)

- Base64
  - `string ToBase64(this string? value, Encoding? encoding = null)`
  - `string FromBase64(this string? value, Encoding? encoding = null)` (คืน `""` เมื่ออินพุตไม่ใช่ Base64)

- Search/Compare
  - `bool EqualsOrdinalIgnoreCase(this string? value, string? other)`
  - `bool ContainsOrdinalIgnoreCase(this string? value, string? part)`
  - `int LevenshteinDistance(this string? a, string? b)`
  - `double SimilarityRatio(this string? a, string? b)` (0.0..1.0)

- Thai Baht Currency Text
  - `string ToThaiBahtText(this string? value)`
    - ใช้ `decimal.TryParse` กับ `CultureInfo.InvariantCulture` หลัง `NormalizeDigits()`; รูปแบบตัวเลขต้องใช้จุดทศนิยม `.`
  - `string ToThaiBahtText(this decimal amount)`
  - `string ToEnglishBahtText(this string? value)`
  - `string ToEnglishBahtText(this decimal amount)`

- Thai Citizen ID
  - `bool IsThaiCitizenId(this string? value)` (ตรวจความถูกต้อง 13 หลัก + checksum)

- Crypto
  - `string ToSha256Hex(this string? value)` (UTF-8 -> SHA-256 -> hex lowercase)

## Notes & Limitations | หมายเหตุ
- `Slugify` ทำงานกับ ASCII เท่านั้น หากอินพุตเป็นภาษาอื่น (เช่น ไทย) และไม่ส่ง `IStringTransliterator` ที่แปลงเป็น ASCII จริง ผลลัพธ์อาจว่างหรือเป็นขีด `-` เท่านั้นหลังการ trim.
- `StripHtml` ใช้ regex แบบง่าย เหมาะสำหรับเคสพื้นฐาน; หาก HTML ซับซ้อน ควรใช้ HTML parser เต็มรูปแบบ.
- `ToThaiBahtText(this string)` ต้องการรูปแบบตัวเลขแบบ Invariant (`1234.56`) หลัง `NormalizeDigits()`; การใช้เครื่องหมายทศนิยม/คั่นหลักแบบ locale อื่นอาจ parse ไม่ผ่าน.
- Similarity/Levenshtein เป็นอัลกอริทึม O(n*m); สำหรับสตริงยาวมากให้พิจารณาแนวทาง incremental/approximate.

## Related Interfaces | อินเทอร์เฟซที่เกี่ยวข้อง
- `Wiz.Utility.Text.IStringSimilarity`
  - `int LevenshteinDistance(string a, string b)`
  - `double Similarity(string a, string b)`
  - Default impl: `Wiz.Utility.Text.DefaultStringSimilarity` (สองแถว DP, คำนวณรวดเร็ว ไม่จัดสรรเกินจำเป็น)
- `Wiz.Utility.Text.IStringTransliterator`
  - `string Transliterate(string input)`
  - Default impl: `Wiz.Utility.Text.DefaultTransliterator` (no-op). เพื่อรองรับภาษา non-Latin ให้เสียบ lib ภายนอกที่ map เป็น ASCII จริง.

## Security | ความปลอดภัย
- อย่าใช้ `FromBase64` กับข้อมูลไม่เชื่อถือเพื่อโค้ดเส้นทางอื่น ๆ โดยไม่ตรวจสอบผลลัพธ์.
- หลีกเลี่ยงการแสดงค่า `ToSha256Hex` ของข้อมูลอ่อนไหวใน log; ใช้กับ salt/pepper ตามแนวปฏิบัติที่ดีเมื่อต้องเก็บ hash ของรหัสผ่าน (แนะนำใช้ฟังก์ชันปรับหน่วงเฉพาะ เช่น PBKDF2/Argon2 แทน SHA-256 ตรง ๆ เมื่อเป็นการยืนยันตัวตน).

## Troubleshooting | แก้ปัญหา
- ผล `Slugify` ว่าง: ตรวจว่าอินพุตมีเพียงตัวอักษร non-ASCII หรือไม่ และ/หรือจัดเตรียม `IStringTransliterator` ที่เหมาะสม.
- `ToThaiBahtText(string)` คืนค่าว่าง: ตรวจรูปแบบตัวเลข (ใช้จุดทศนิยม `.`) และพิจารณา parse เองเป็น `decimal` ก่อนเรียกโอเวอร์โหลดแบบ `decimal`.
- ความคล้ายช้าเมื่อสตริงยาวมาก: พิจารณา truncate/approximate หรือโครงสร้างข้อมูลเฉพาะทาง.
