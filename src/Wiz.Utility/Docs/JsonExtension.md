# JsonExtension — การจัดรูปแบบ JSON แบบยืดหยุ่น (System.Text.Json / Newtonsoft.Json)

เอกสารสรุปการใช้งาน `JsonExtension` สำหรับซีเรียไลซ์/ดีซีเรียไลซ์ JSON โดยเลือกเอนจินได้ทั้ง `System.Text.Json` (STJ) และ `Newtonsoft.Json` (Json.NET) พร้อมปรับแต่งรูปแบบชื่อ (naming case), การแปลง `enum` เป็น string และรูปแบบวันที่ได้อย่างยืดหยุ่น

Last updated: 2025-08-16

## Overview | ภาพรวม
- รองรับ 2 เอนจิน: `System.Text.Json` และ `Newtonsoft.Json`
- ตั้งค่า naming ได้: `PascalCase`, `CamelCase`, `snake_case` (lower/UPPER), `kebab-case` (lower/UPPER)
- เลือก serialize `enum` เป็น string (คงค่า numeric ได้โดยปิดตัวเลือก)
- กำหนด `dateFormat` สำหรับ `DateTime`/`DateTimeOffset` ได้ (เขียนออกด้วย `InvariantCulture` เพื่อผลลัพธ์เสถียรข้าม locale)
 - โหมด auto-detect ชื่อคีย์ JSON ระหว่าง `camelCase`/`PascalCase`/`snake_case`/`kebab-case` โดยใช้ `JsonCase.Auto` (heuristic-based)

ไฟล์ที่เกี่ยวข้อง:
- `src/Wiz.Utility/Extensions/JsonExtension.cs`
- `src/Wiz.Utility/Extensions/JsonExtension.Serialization.cs`
- `src/Wiz.Utility/Extensions/JsonExtension.NewtonsoftNaming.cs`

Target framework: `.NET 8.0` (ดู `src/Wiz.Utility/Wiz.Utility.csproj`)

## Prerequisites | ข้อกำหนดเบื้องต้น
- .NET SDK 8.0+
- แพ็กเกจ `Newtonsoft.Json` 13.0.3 (อ้างอิงไว้ใน `Wiz.Utility.csproj` แล้ว)

## Quickstart | เริ่มใช้งานรวดเร็ว
```csharp
using Wiz.Utility.Extensions;

// Sample DTO
public enum OrderStatus { New, Processing, Done }
public sealed class Order
{
    public string? OrderId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public OrderStatus Status { get; set; }
}

var order = new Order
{
    OrderId = "A-001",
    CreatedUtc = DateTime.UtcNow,
    Status = OrderStatus.Processing
};

// 1) Serialize with System.Text.Json + camelCase + enum as string + custom date format
string json1 = order.ToJson(
    JsonExtension.JsonEngine.SystemTextJson,
    JsonExtension.JsonCase.CamelCase,
    indented: true,
    dateFormat: "yyyy-MM-dd'T'HH:mm:ss.fffK",
    enumAsString: true);

// 2) Deserialize (generic)
Order? back1 = json1.FromJson<Order>(
    JsonExtension.JsonEngine.SystemTextJson,
    JsonExtension.JsonCase.CamelCase,
    dateFormat: "yyyy-MM-dd'T'HH:mm:ss.fffK",
    enumAsString: true);

// 3) Serialize with Newtonsoft.Json + SNAKE_CASE + enum as string
string json2 = order.ToJson(
    JsonExtension.JsonEngine.NewtonsoftJson,
    JsonExtension.JsonCase.SnakeCaseUpper,
    indented: false,
    dateFormat: "yyyy-MM-dd'T'HH:mm:ssK",
    enumAsString: true);

// 4) Deserialize to a non-generic type
object? back2 = json2.FromJson(
    typeof(Order),
    JsonExtension.JsonEngine.NewtonsoftJson,
    JsonExtension.JsonCase.SnakeCaseUpper,
    dateFormat: "yyyy-MM-dd'T'HH:mm:ssK",
    enumAsString: true);

// 5) Create options/settings factories
var stjOptions = JsonExtension.CreateStjOptions(
    JsonExtension.JsonCase.KebabCaseLower, indented: true, enumAsString: true);
var newtonsoftSettings = JsonExtension.CreateNewtonsoftSettings(
    JsonExtension.JsonCase.CamelCase, indented: false, enumAsString: true);
```

ตัวอย่าง Auto-detect (อ่าน JSON แล้วเดา naming อัตโนมัติ):
```csharp
// Will detect naming from JSON keys, then use it for deserialization
Order? autoBack = json1.FromJson<Order>(
    JsonExtension.JsonEngine.SystemTextJson,
    JsonExtension.JsonCase.Auto);
```

## API Summary | สรุป API
- Enums
  - `JsonExtension.JsonEngine`
    - `SystemTextJson`
    - `NewtonsoftJson`
  - `JsonExtension.JsonCase`
    - `Auto`, `PascalCase`, `CamelCase`, `SnakeCaseLower`, `SnakeCaseUpper`, `KebabCaseLower`, `KebabCaseUpper`

- Methods
  - `string ToJson(this object? value, JsonEngine engine = SystemTextJson, JsonCase naming = CamelCase, bool indented = false, string? dateFormat = null, bool enumAsString = true)`
    - Serialize object เป็น JSON โดยเลือกเอนจิน, รูปแบบชื่อ, จัดย่อหน้า, รูปแบบวันที่, และ enum เป็น string
  - `T? FromJson<T>(this string json, JsonEngine engine = SystemTextJson, JsonCase naming = CamelCase, string? dateFormat = null, bool enumAsString = true)`
    - Deserialize JSON เป็นชนิด `T`
  - `object? FromJson(this string json, Type returnType, JsonEngine engine = SystemTextJson, JsonCase naming = CamelCase, string? dateFormat = null, bool enumAsString = true)`
    - Deserialize ไปยังชนิด runtime
  - `JsonSerializerOptions CreateStjOptions(JsonCase naming = CamelCase, bool indented = false, string? dateFormat = null, bool enumAsString = true)`
    - สร้าง `System.Text.Json.JsonSerializerOptions` ที่ตั้งค่าพร้อมใช้งาน
  - `JsonSerializerSettings CreateNewtonsoftSettings(JsonCase naming = CamelCase, bool indented = false, string? dateFormat = null, bool enumAsString = true)`
    - สร้าง `Newtonsoft.Json.JsonSerializerSettings` ที่ตั้งค่าพร้อมใช้งาน

## Notes & Limitations | หมายเหตุ/ข้อจำกัด
- STJ snake/kebab naming (`SnakeCaseLower/Upper`, `KebabCaseLower/Upper`) ใช้ได้บน .NET 8+ เท่านั้น
- สำหรับ Newtonsoft: `SnakeCaseLower` ใช้ `SnakeCaseNamingStrategy` และสำหรับ `SNAKE_CASE`/`KEBAB-CASE` แบบ upper/lower ใช้ `DelimitedCaseNamingStrategy` ภายในไฟล์ `JsonExtension.NewtonsoftNaming.cs`
- เมื่อระบุ `dateFormat`, การเขียนจะใช้ `CultureInfo.InvariantCulture` เพื่อผลลัพธ์เสถียรข้ามเครื่อง/โลเคล
- เปิด `PropertyNameCaseInsensitive = true` ใน STJ เพื่อความยืดหยุ่นในการอ่าน

## Troubleshooting | แนวทางแก้ปัญหา
- ชนชื่อ type ระหว่าง STJ vs Newtonsoft (เช่น `JsonSerializer`, `JsonConverter<T>`)
  - ให้ใช้ชื่อเต็ม เช่น `System.Text.Json.JsonSerializer` หรือ `System.Text.Json.Serialization.JsonConverter<T>`
- Enum ไม่ถูก serialize เป็น string
  - ตรวจสอบว่า `enumAsString = true` และ naming ของ enum ตรงตามที่ต้องการ
- วันที่ parse ไม่ตรง
  - ยืนยันว่า `dateFormat` ฝั่ง serialize/deserialize เหมือนกัน หากไม่ระบุ จะมี fallback ไป parse ปกติของ BCL

## Security | ความปลอดภัย
- ไม่มีการจัดเก็บ secrets
- ตรวจสอบ/validate input JSON เสมอเมื่อข้อมูลมาจากแหล่งภายนอก
- หลีกเลี่ยงการ throw ทิ้งโดยไม่จับ; พิจารณาเพิ่ม logging ในโค้ดที่เรียกใช้งาน

## Related | ที่เกี่ยวข้อง
- `System.Text.Json` naming policies (.NET 8+): snake/kebab
- Newtonsoft.Json `NamingStrategy` และ `StringEnumConverter`

