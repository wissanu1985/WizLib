# README #

This README would normally document whatever steps are necessary to get your application up and running.

## Wiz.Utility — .NET extension utilities

ชุด Extension Methods สำหรับงานทั่วไปใน .NET/C#: วันที่/เวลา, ข้อความ, ตัวเลข, JSON (System.Text.Json/Newtonsoft), และการ map วัตถุแบบ lightweight. โฟกัสที่ใช้งานง่าย, ปลอดภัยต่อ null, ไม่พึ่งไลบรารีภายนอกเกินจำเป็น.

### Quickstart
Prerequisites: .NET SDK 8.0+

- เพิ่ม project reference ไปยัง `src/Wiz.Utility/Wiz.Utility.csproj`
- นำเข้าเนมสเปซที่ต้องใช้:

```csharp
using Wiz.Utility.Extensions;
using Wiz.Utility.Text; // เมื่อต้องใช้ IStringTransliterator/DefaultTransliterator
```

ตัวอย่างสั้น ๆ:

```csharp
// DateTime helpers
var now = DateTime.Now;
var startOfWeek = now.StartOfWeek(DayOfWeek.Monday);

// String helpers
"HelloWorld".ToSnakeCase(); // "hello_world"
"1101700203451".IsThaiCitizenId(); // ตรวจเลขบัตรประชาชนไทย

// Numeric helpers
1536L.ToHumanSize(); // "1.5 KB"

// JSON (เลือก STJ หรือ Newtonsoft + กำหนด naming/date format)
var dto = new { OrderId = "A-001", CreatedUtc = DateTime.UtcNow };
string json = dto.ToJson(
    JsonExtension.JsonEngine.SystemTextJson,
    JsonExtension.JsonCase.CamelCase,
    indented: true,
    dateFormat: "yyyy-MM-dd'T'HH:mm:ss.fffK",
    enumAsString: true);

// Object mapping (lightweight)
var person = new { Id = "5", Name = "Alice", Age = "42" }.Adapt<Person>();
```

### Features
- __DateTimeExtensions__: ขอบเขตวัน/สัปดาห์/เดือน/ปี, ISO week/year, business days, ISO8601/Unix, time zone, Thai Buddhist format
- __StringExtensions__: null/empty helpers, safe substring, normalize whitespace/diacritics/digits, case/snake/kebab/slug, Base64, Thai/English Baht text, Thai citizen ID, Levenshtein/similarity, SHA-256
- __NumericExtensions__: even/odd, range check/Clamp, NearlyEquals (double/decimal), map range, ordinal, human-readable size, rounding, safe divide
- __JsonExtension__: ซีเรียไลซ์/ดีซีเรียไลซ์ด้วย STJ/Newtonsoft, naming policies (camel/snake/kebab), enum-as-string, custom date format, auto-detect naming
- __ObjectMappingExtension__: map ตามชื่อพร็อพเพอร์ตี (case-insensitive), scalar/collection conversions, custom converters, ignore, failure policies, cycle detection

### Documentation
- `src/Wiz.Utility/Docs/DateTimeExtensions.md`
- `src/Wiz.Utility/Docs/StringExtensions.md`
- `src/Wiz.Utility/Docs/NumericExtensions.md`
- `src/Wiz.Utility/Docs/JsonExtension.md`
- `src/Wiz.Utility/Docs/ObjectMappingExtension.md`

### Testing
- รันชุดทดสอบทั้งหมด: `dotnet test`
- ดูโค้ดทดสอบตัวอย่างในโฟลเดอร์ `tests/Wiz.Utility.Test/`

### Notes
- Target framework: .NET 8.0
- ไม่มีการเก็บ secrets ในไลบรารีนี้ และหลีกเลี่ยง dependency ภายนอกที่ไม่จำเป็น

### Who do I talk to? ###

* Repo owner or admin
* Other community or team contact