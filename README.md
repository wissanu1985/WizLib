# Wiz.Utility — Utility Extensions for .NET/C#

ชุดส่วนขยาย (extension methods) สำหรับงานพื้นฐานใน .NET/C# ที่ใช้ง่าย ปลอดภัยต่อ null และลดการพึ่งพาแพ็กเกจภายนอก เน้น DateTime/String/Numeric/JSON/Object Mapping และ LINQ helpers ประสิทธิภาพดี

---

## Badges

![Build](https://img.shields.io/badge/build-passing-brightgreen.svg)
![Tests](https://img.shields.io/badge/tests-passing-brightgreen.svg)
![Coverage](https://img.shields.io/badge/coverage-89.4%25-blue.svg)
![License: MIT](https://img.shields.io/badge/license-MIT-yellow.svg)
![NuGet](https://img.shields.io/badge/nuget-soon-blueviolet.svg)

---

## Quickstart (≤ 60s)

- Prerequisite: .NET SDK 8.0+
- เพิ่ม project reference ไปที่ `src/Wiz.Utility/Wiz.Utility.csproj`

```bash
dotnet add <YourProject>.csproj reference src/Wiz.Utility/Wiz.Utility.csproj
```

นำเข้าเนมสเปซที่จำเป็น:

```csharp
using Wiz.Utility.Extensions;
using Wiz.Utility.Text; // สำหรับ IStringTransliterator/DefaultTransliterator
```

ตัวอย่างสั้น (DateTime/String/Numeric/JSON/Mapping/LINQ):

```csharp
using System;
using System.Linq;
using Wiz.Utility.Extensions;

// DateTime
var sod = DateTime.Now.StartOfDay();

// String
var slug = "Café déjà vu!".Slugify(); // "cafe-deja-vu"

// Numeric
var size = 1536L.ToHumanSize(); // "1.5 KB"

// JSON (System.Text.Json + camelCase)
var dto = new { OrderId = "A-001", CreatedUtc = DateTime.UtcNow };
var json = dto.ToJson(JsonExtension.JsonEngine.SystemTextJson, JsonExtension.JsonCase.CamelCase, indented: false, dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'", enumAsString: true);

// Mapping
var person = new { Id = "5", Name = "Alice", Age = "42" }.Adapt<Person>();

// LINQ helpers
var data = Enumerable.Range(0, 10);
var squaresOfEven = data.WhereSelectToList(i => (i & 1) == 0, i => i * i);
```

---

## Features

- __DateTime__: Start/End วัน/สัปดาห์/เดือน/ปี, ISO week/year, business days, ISO/Unix, time zones, Thai Buddhist format
- __String__: null/empty helpers, safe substring, normalize whitespace/diacritics/digits, snake/kebab/slug, Base64, Thai/English Baht text, Thai citizen ID, Levenshtein/similarity, SHA-256
- __Numeric__: even/odd, IsBetween/Clamp, NearlyEquals (double/decimal), map range, ordinal, human-readable size, rounding, safe divide
- __JSON__: เลือก STJ/Newtonsoft ได้, naming policies (camel/snake/kebab), enum-as-string, custom date format, auto-detect naming
- __Object Mapping__: map ตามชื่อพร็อพเพอร์ตี, scalar/collection conversions, custom converters, ignore/failure policies, cycle handling
- __LINQ__: `TryFirst/TrySingle`, `ToArrayFast/ToListFast`, `SelectToList/WhereSelectToList`, `ForEachFast` และเวอร์ชัน async

---

## Installation

- ใช้งานภายในโซลูชัน: อ้างอิงโปรเจกต์โดยตรง

```bash
dotnet add <YourProject>.csproj reference src/Wiz.Utility/Wiz.Utility.csproj
```

- NuGet: ยังไม่ปล่อยสู่ NuGet (วางแผนในอนาคต). เมื่อพร้อมจะปรับ README และเพิ่ม badge อัตโนมัติ

---

## Usage (ตัวอย่างเพิ่มจาก Quickstart)

โค้ดด้านล่างแสดงตัวอย่างการใช้งานพร้อมคอมเมนต์ (คำอธิบายเป็นภาษาไทย แต่โค้ด/คอมเมนต์เป็นภาษาอังกฤษ):

```csharp
using System;
using System.Globalization;
using Wiz.Utility.Extensions;

// Business days with holidays
var holidays = new HashSet<DateOnly> { new(2025, 4, 14), new(2025, 4, 15) };
var due = new DateTime(2025, 4, 11).AddBusinessDays(3, holidays); // skip Thai New Year

// Currency text (Thai/English)
string thText = 1234.56m.ToThaiBahtText();
string enText = 1234.56m.ToEnglishBahtText();

// JSON with Newtonsoft + SNAKE_CASE (upper)
var json2 = new { OrderId = "X", CreatedUtc = DateTime.UtcNow }.ToJson(
    JsonExtension.JsonEngine.NewtonsoftJson,
    JsonExtension.JsonCase.SnakeCaseUpper,
    indented: true,
    dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
    enumAsString: true);
```

ดูตัวอย่างเชิงลึกเพิ่มเติมในไฟล์เอกสารด้านล่าง

---

## Documentation

- [src/Wiz.Utility/Docs/DateTimeExtensions.md](src/Wiz.Utility/Docs/DateTimeExtensions.md)
- [src/Wiz.Utility/Docs/StringExtensions.md](src/Wiz.Utility/Docs/StringExtensions.md)
- [src/Wiz.Utility/Docs/NumericExtensions.md](src/Wiz.Utility/Docs/NumericExtensions.md)
- [src/Wiz.Utility/Docs/JsonExtension.md](src/Wiz.Utility/Docs/JsonExtension.md)
- [src/Wiz.Utility/Docs/ObjectMappingExtension.md](src/Wiz.Utility/Docs/ObjectMappingExtension.md)
- [src/Wiz.Utility/Docs/LinqExtension.md](src/Wiz.Utility/Docs/LinqExtension.md)

---

## Testing & Coverage

- รันชุดทดสอบ: `dotnet test -c Release`
- รายงาน coverage: สร้างด้วย pipeline มาตรฐานของ .NET (เช่น Coverlet + ReportGenerator)
- ตัวเลขล่าสุด (จาก `coveragereport/Summary.txt`):
  - Line coverage: 89.4%
  - Branch coverage: 79.5%
  - Method coverage: 96.9%
- ไฮไลต์รายคลาส (เด่นสุด):
  - `Wiz.Utility.Extensions.MappingError` — 100%
  - `Wiz.Utility.Text.DefaultStringSimilarity` — 100%
  - `Wiz.Utility.Text.DefaultTransliterator` — 100%
- ดูรายละเอียด: [coveragereport/Summary.txt](coveragereport/Summary.txt)

---

## Dependencies

- Target framework: `net8.0`
- NuGet packages (จาก `src/Wiz.Utility/Wiz.Utility.csproj`):
  - `Newtonsoft.Json` 13.0.3
- Built-ins / BCL ที่ใช้: รองรับ `System.Text.Json` เป็นเอนจิน JSON ทางเลือก
- แนวทางการออกแบบ: ลด dependency ภายนอกที่ไม่จำเป็น และไม่มีการเก็บ secrets ในที่เก็บซอร์ส

---

## Security

- ไม่มีการเก็บความลับ (secrets) ในรีโป
- ปฏิบัติตามแนวทาง .NET ปกติในการจัดการค่า config/secrets (เช่น User Secrets/Environment variables)

---

## Contributing

ยินดีรับ Issue/PR. จะเพิ่มไฟล์ CONTRIBUTING.md ภายหลังเพื่ออธิบายขั้นตอนและมาตรฐานคอมมิต (Conventional Commits)

---

## License

SPDX-License-Identifier: MIT — ดูไฟล์ [LICENSE](LICENSE)
