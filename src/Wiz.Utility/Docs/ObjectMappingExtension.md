# ObjectMappingExtension

เอกสารนี้อธิบายการใช้งาน extension methods สำหรับการ map วัตถุแบบ lightweight ภายใต้เนมสเปซ `Wiz.Utility.Extensions` โดยไม่ต้องพึ่ง 3rd-party mapper ใหญ่ ๆ

- Class/Extension: `ObjectMappingExtension`
- Options: `MappingOptions`, `ConversionFailureBehavior`, `MappingError`
- คุณสมบัติเด่น
  - __Map ตามชื่อพร็อพเพอร์ตี__ (public instance, non-indexer) แบบ __case-insensitive__ โดยค่าเริ่มต้น
  - __Scalar conversions__ ครอบคลุม: primitives, enums, `string`, `decimal`, `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan` (รวม parse จาก string หลายรูปแบบ เช่น ISO Round-trip "O") และ bool จาก "1"/"0"
  - __Collections__ รองรับ `T[]` และ `List<T>` (และ `IEnumerable<T>`): สร้างอาร์เรย์ใหม่, เติม/เคลียร์ `List<T>` เดิมตามกรณี
  - __Custom converters__ ผ่าน `MappingOptions.AddConverter<TSrc, TDest>()`
  - __Ignore__ บางพร็อพเพอร์ตีปลายทางด้วยชื่อ (`MappingOptions.Ignore`)
  - __นโยบายเมื่อแปลงไม่ได้__ (`ConversionFailureBehavior`): SetNullOrDefault, Skip, Throw
  - __Cycle detection__ สำหรับ object graph อ้างอิงกันเป็นวง เพื่อหลีกเลี่ยง StackOverflow และคงโหนดที่เคย map แล้ว

## Quick start

```csharp
using Wiz.Utility.Extensions;

public sealed class PersonDto { public int Id { get; set; } public string? Name { get; set; } public string? Age { get; set; } }
public sealed class Person    { public int Id { get; set; } public string? Name { get; set; } public int Age { get; set; } }

var dto = new PersonDto { Id = 5, Name = "Alice", Age = "42" };
var person = dto.Adapt<Person>();
// person.Id == 5; person.Name == "Alice"; person.Age == 42
```

## Public API

- `TDestination? Adapt<TDestination>(this object? source, MappingOptions? options = null)`
  - Map `source` ไปเป็นอินสแตนซ์ใหม่ชนิด `TDestination`. ถ้า `source` เป็น `null` จะได้ `null` สำหรับ reference/nullable หรือ default(T) สำหรับ value type

- `object? Adapt(this object? source, Type destinationType, MappingOptions? options = null)`
  - เหมือนด้านบน แต่ระบุชนิดเป้าหมายด้วย `Type`

- `TDestination AdaptInto<TDestination>(this object? source, TDestination destination, MappingOptions? options = null) where TDestination : class`
  - Map ใส่วัตถุ `destination` ที่มีอยู่แล้ว (in-place) และคืนค่าออบเจ็กต์เดิมนั้น

## MappingOptions (สำคัญ)

```csharp
var opts = new MappingOptions
{
    CaseSensitive = false,               // เปรียบเทียบชื่อพร็อพเพอร์ตีไม่สนตัวพิมพ์ใหญ่เล็ก (default: false)
    IgnoreNullValues = false,            // ข้ามการตั้งค่าค่า null จาก source (default: false)
    StrictMode = false,                  // โยนข้อผิดพลาดออกจาก ErrorHandler ทันที (default: false)
    ConversionFailure = ConversionFailureBehavior.SetNullOrDefault,
    DateTimeFormats = null,              // ถ้า null ใช้ชุด DefaultDateTimeFormats ภายใน
    ErrorHandler = err => { /* log */ }, // เรียกเมื่อพบข้อผิดพลาดระหว่าง mapping
};

opts.Ignore("Password"); // ไม่ map property ปลายทางชื่อ Password

opts.AddConverter<string, int>(s => int.Parse(s ?? "0"));
```

- `ConversionFailureBehavior`
  - `SetNullOrDefault`: ตั้งค่า default(T) หรือ null เมื่อแปลงไม่ได้ (ค่าเริ่มต้น)
  - `Skip`: ข้ามการตั้งค่าสำหรับสมาชิกนั้น (ค่าที่มีอยู่ในปลายทางยังคงเดิม)
  - `Throw`: โยน `InvalidCastException`

- `DateTimeFormats`
  - หากระบุจะใช้รูปแบบเหล่านี้ก่อน เมื่อ parse `DateTime`/`DateTimeOffset` จาก string; หากไม่ระบุจะใช้ `MappingOptions.DefaultDateTimeFormats` ซึ่งรวม "O", `yyyy-MM-dd`, `yyyy/MM/dd`, `MM/dd/yyyy`, `dd/MM/yyyy`

- `ErrorHandler`
  - เรียกเมื่อเกิด exception ระหว่าง map สมาชิกหนึ่ง ๆ พร้อมข้อมูล `MappingError`. ถ้า `StrictMode = true` จะ rethrow หลังเรียก handler

## Collections behavior

- เมื่อ map ไปยังอาร์เรย์ปลายทาง (`T[]`): จะสร้างอาร์เรย์ใหม่ทุกครั้ง
- เมื่อ map ไปยัง `List<T>` ที่ปลายทางมีอยู่แล้ว: พยายาม `Clear()` และเติมค่าที่ map แล้ว (ถ้า `Clear()` ไม่ได้ จะ fallback ใส่ทับต่อท้ายตามกรณีที่รองรับ)
- รองรับการแปลงชนิดคอลเลกชันระหว่างกัน เช่น `List<Src>` -> `List<Dest>`, `Src[]` -> `List<Dest>`, `List<Src>` -> `Dest[]`

ตัวอย่าง:

```csharp
public sealed class SrcWrap { public List<PersonDto>? Items { get; set; } }
public sealed class DstWrap { public List<Person>? Items { get; set; } }

var src = new SrcWrap { Items = new() { new PersonDto { Id = 1, Age = "30" } } };
var dst = src.Adapt<DstWrap>();
// dst.Items!.Count == 1; dst.Items[0].Age == 30
```

## Case sensitivity

- ค่าเริ่มต้นคือ __ไม่สนตัวพิมพ์ใหญ่เล็ก__ (`CaseSensitive = false`)

```csharp
public sealed class CaseSrc { public string? firstName { get; set; } }
public sealed class CaseDst { public string? FirstName { get; set; } }

var src = new CaseSrc { firstName = "Ann" };
var dst = src.Adapt<CaseDst>();
// dst.FirstName == "Ann"

var strict = src.Adapt<CaseDst>(new MappingOptions { CaseSensitive = true });
// strict.FirstName == null
```

## Custom converters

```csharp
var opts = new MappingOptions()
    .AddConverter<string, int>(s => int.Parse(s ?? "0"))
    .AddConverter<Animal, string>(a => $"Animal:{a?.Name}");

var dog = new Dog { Name = "Rex", Age = 3 };
var anonDest = new { Name = (string?)null };
var mapped = dog.Adapt(anonDest.GetType(), opts);
// mapped.Name == "Animal:Rex"
```

- Converter ที่ลงทะเบียนด้วย base type (เช่น `Animal`) จะถูกใช้กับ derived source (เช่น `Dog`) อัตโนมัติ

## Error handling และ Strict mode

```csharp
var called = 0;
var opts = new MappingOptions
{
    ErrorHandler = _ => called++,
    StrictMode = false
}.AddConverter<string, int>(_ => throw new InvalidOperationException("boom"));

var dest = new { Age = 0 };
var result = new { Age = "bad" }.Adapt(dest.GetType(), opts);
// ErrorHandler จะถูกเรียก, การ map ดำเนินต่อไป และค่าที่แปลงไม่สำเร็จจะเป็น default ตามนโยบาย
```

- หาก `StrictMode = true` ข้อผิดพลาดจะถูกโยนออกหลัง `ErrorHandler` ถูกเรียก

## Conversion policies (เมื่อ parse/แปลงไม่ได้)

```csharp
var src = new { N = "not-a-number" };

// Default: SetNullOrDefault
var d1 = src.Adapt(new { N = 0 }.GetType());
// N == 0

// Skip: รักษาค่าเดิมในปลายทาง
var existing = new { N = 123 };
var d2 = src.Adapt(existing.GetType(), new MappingOptions { ConversionFailure = ConversionFailureBehavior.Skip });

// Throw
// Should throw InvalidCastException เมื่อตั้ง ConversionFailure = Throw
```

## DateTime/DateTimeOffset/TimeSpan

- รองรับ parse จาก string หลายรูปแบบ (รวม Round-trip "O"), หากล้มเหลวจะปฏิบัติตาม `ConversionFailure`

```csharp
var iso = DateTime.UtcNow.ToString("O");
var d = new { When = iso }.Adapt(new { When = default(DateTime) }.GetType());
```

## Mapping into existing instance

```csharp
var src = new { Name = (string?)null, Age = "99" };
var dest = new Person { Id = 7, Name = "keep", Age = 1 };

src.AdaptInto(dest, new MappingOptions { IgnoreNullValues = true });
// dest.Name == "keep" (null ถูกข้าม), dest.Age == 99
```

## Cycle handling

- มีตัวตรวจจับอ้างอิงซ้ำ (reference tracking) สำหรับออบเจ็กต์อ้างกันเป็นวง เพื่อคงโครงสร้างกราฟปลายทางอย่างถูกต้องและป้องกัน stack overflow

## ข้อจำกัด/หมายเหตุ

- Mapping ทำงานกับพร็อพเพอร์ตีแบบ public instance เท่านั้น (ไม่รองรับ indexer)
- ปลายทางที่เป็นอาร์เรย์จะถูกสร้างอินสแตนซ์ใหม่เสมอ (ไม่แก้ไขอาร์เรย์เดิม in-place)
- การเลือกชนิดคอลเลกชันปลายทางที่ไม่ใช่ `T[]` หรือ `List<T>` จะถูก materialize เป็น `List<T>` ตามองค์ประกอบปลายทาง

## ตัวอย่างรวม (ครบเครื่อง)

```csharp
using Wiz.Utility.Extensions;
using System;
using System.Collections.Generic;

public sealed class Src
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? When { get; set; } // DateTime string
    public List<string>? Scores { get; set; } // numbers as strings
}

public sealed class Dst
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateTime When { get; set; }
    public List<int>? Scores { get; set; }
}

var opts = new MappingOptions()
    .AddConverter<string, int>(s => int.Parse(s ?? "0"));

var src = new Src
{
    Id = "1001",
    Name = "Alice",
    When = DateTime.UtcNow.ToString("O"),
    Scores = new() { "1", "2", "3" }
};

var dst = src.Adapt<Dst>(opts);
// dst.Id == 1001; dst.Scores: [1,2,3]
```

---

อ้างอิงซอร์สโค้ดหลัก: `src/Wiz.Utility/Extensions/ObjectMappingExtension.cs` และชุดทดสอบตัวอย่างการใช้งาน: `tests/Wiz.Utility.Test/Extensions/ObjectMappingExtensionTests.cs`
