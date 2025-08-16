# ObjectMappingExtension

เอกสารนี้อธิบายการใช้งาน extension methods สำหรับการ map วัตถุแบบ lightweight ภายใต้เนมสเปซ `Wiz.Utility.Extensions` โดยไม่ต้องพึ่ง 3rd-party mapper ใหญ่ ๆ
 
---

## Advanced-first

- __[Constructor mapping]__ ปลายทางไม่มี parameterless ctor ก็ยังสร้างได้ หากพารามิเตอร์ ctor จับคู่ชื่อ/ชนิดจาก source และสามารถแปลงชนิดได้

```csharp
using Wiz.Utility.Extensions;

public sealed class CtorOnlyDest
{
    public int Id { get; }
    public string? Name { get; }
    public CtorOnlyDest(int id, string? name) { Id = id; Name = name; }
}

var src = new { Id = "10", Name = "Zed" }; // string -> int
var dest = src.Adapt<CtorOnlyDest>();
// dest.Id == 10; dest.Name == "Zed"
```

- __[Array vs List ใน AdaptInto]__
  - เมื่อปลายทางเป็น `T[]` อาร์เรย์จะถูก __สร้างใหม่__ เสมอ (ไม่แก้ไขอินสแตนซ์เดิม)
  - เมื่อปลายทางเป็น `List<T>` จะพยายาม `Clear()` แล้วเติมรายการที่ map แล้ว

```csharp
public sealed class HoldSrc { public int[]? Nums { get; set; } }
public sealed class HoldDst { public int[]? Nums { get; set; } }

var src2 = new HoldSrc { Nums = new[] { 1, 2 } };
var dst2 = new HoldDst { Nums = new[] { 999 } };
src2.AdaptInto(dst2);
// dst2.Nums != old array instance; dst2.Nums.SequenceEqual(new[]{1,2})
```

- __[IgnoreNullValues กับ default ของ value-types]__
  - เมื่อ `IgnoreNullValues = true` และค่า source เป็น `null` จะ __ข้าม__ การตั้งค่าบนปลายทาง
  - หาก source มีค่าเป็น default ของ value-type (เช่น 0 สำหรับ `int`) ระบบจะข้ามเช่นกัน เพื่อไม่ทับค่าที่มีอยู่ในปลายทางโดยไม่ตั้งใจ

```csharp
var src3 = new { Name = (string?)null, Age = 0 }; // 0 treated as default
var dst3 = new { Name = "keep", Age = 42 };
var mapped3 = src3.Adapt(dst3.GetType(), new MappingOptions { IgnoreNullValues = true });
```

- __[Anonymous type destinations]__ สามารถระบุปลายทางด้วย `Type` ของ anonymous ได้ สะดวกสำหรับ Compose ผลลัพธ์เฉพาะกิจ

```csharp
var destShape = new { When = default(DateTimeOffset), Name = (string?)null }.GetType();
var outObj = new { When = DateTimeOffset.UtcNow.ToString("O"), Name = "A" }
    .Adapt(destShape);
```

- __[Conversion policy สรุปสั้น]__ เมื่อแปลงสเกลาร์ไม่ได้:
  - `SetNullOrDefault` -> ตั้งค่า default(T)/null
  - `Skip` -> เก็บค่าปลายทางเดิมไว้ (ใช้กับ `AdaptInto` ชัดเจน)
  - `Throw` -> โยน `InvalidCastException`

---

## Public API (สรุป)

- `TDestination? Adapt<TDestination>(object? source, MappingOptions? options = null)`
- `object? Adapt(object? source, Type destinationType, MappingOptions? options = null)`
- `TDestination AdaptInto<TDestination>(object? source, TDestination destination, MappingOptions? options = null) where TDestination : class`

หมายเหตุ: ถ้า `source` เป็น `null` จะได้ `null` (สำหรับ reference/nullable) หรือ default(T) (สำหรับ value-type)

## Quick start (ย่อ)

```csharp
using Wiz.Utility.Extensions;

public sealed class PersonDto { public int Id { get; set; } public string? Name { get; set; } public string? Age { get; set; } }
public sealed class Person    { public int Id { get; set; } public string? Name { get; set; } public int Age { get; set; } }

var dto = new PersonDto { Id = 5, Name = "Alice", Age = "42" };
var person = dto.Adapt<Person>();
```

## Collections

- รองรับ `T[]`, `List<T>`, และ `IEnumerable<T>`
- แปลงข้ามชนิดคอลเลกชัน เช่น `List<Src>` -> `Dest[]`, `Src[]` -> `List<Dest>`

```csharp
public sealed class BoxS { public List<PersonDto>? Items { get; set; } }
public sealed class BoxD { public List<Person>? Items { get; set; } }

var b = new BoxS { Items = new() { new PersonDto { Id = 1, Age = "30" } } };
var mapped = b.Adapt<BoxD>();
```

## Date/Guid/Bool conversions

- DateTime/DateTimeOffset: รองรับหลายรูปแบบ รวม "O"; บังคับรูปแบบด้วย `MappingOptions.DateTimeFormats`
- Guid: จาก string (valid)
- Bool: จาก string "true"/"false" และ "1"/"0"

```csharp
var d = new { When = DateTime.UtcNow.ToString("O") }.Adapt(new { When = default(DateTime) }.GetType());
var g = new { G = Guid.NewGuid().ToString() }.Adapt(new { G = Guid.Empty }.GetType());
var b2 = new { B = "1" }.Adapt(new { B = false }.GetType());
```

## Custom converters

```csharp
var opts = new MappingOptions()
    .AddConverter<string, int>(s => int.Parse(s ?? "0"))
    .AddConverter<Animal, string>(a => $"Animal:{a?.Name}");
```

หมายเหตุ: Converter ของ base type จะถูกใช้กับ derived type อัตโนมัติ

## Error handling / Strict mode

```csharp
var opts = new MappingOptions
{
    ErrorHandler = err => Console.WriteLine($"{err.DestinationMember}: {err.Exception.Message}"),
    StrictMode = false
};
```

- เมื่อ `StrictMode = true` จะ rethrow error หลังเรียก `ErrorHandler`

## Cycle handling

- มี reference tracking ป้องกัน StackOverflow และรักษาวงจรใน object graph

## ข้อจำกัด

- Map เฉพาะ public instance properties แบบ non-indexer
- อาร์เรย์ปลายทางถูกสร้างใหม่เสมอ
- คอลเลกชันปลายทางประเภทอื่นจะถูก materialize เป็น `List<T>` ตาม element type

---

อ้างอิงซอร์สโค้ดหลัก: `src/Wiz.Utility/Extensions/ObjectMappingExtension.cs` และชุดทดสอบตัวอย่างการใช้งาน: `tests/Wiz.Utility.Test/Extensions/ObjectMappingExtensionTests.cs`
