# JsonExtension — จัดการ JSON ได้ทั้ง System.Text.Json และ Newtonsoft.Json อย่างยืดหยุ่น

สรุปการใช้งาน `JsonExtension` สำหรับ serialize/deserialize JSON โดยเลือกเอนจินได้ทั้ง `System.Text.Json` (STJ) และ `Newtonsoft.Json` (Json.NET) พร้อมปรับแต่ง naming (Pascal/Camel/Snake/Kebab), `enum` เป็น string/number, และ `dateFormat` ได้อย่างยืดหยุ่น

Last updated: 2025-08-16

## Overview | ภาพรวม
- รองรับ 2 เอนจิน: `System.Text.Json` และ `Newtonsoft.Json`
- ตั้งค่า naming: `PascalCase`, `CamelCase`, `SnakeCaseLower`, `SnakeCaseUpper`, `KebabCaseLower`, `KebabCaseUpper`
- `enumAsString` เลือก serialize enum เป็น string (เช่น "Processing") หรือ numeric (เช่น 2)
- `dateFormat` ระบุรูปแบบวันที่เวลาได้ (ใช้ InvariantCulture เมื่อเขียน)
- `JsonCase.Auto` ตรวจจับ naming ของ JSON โดยอัตโนมัติขณะอ่าน

ไฟล์ที่เกี่ยวข้อง:
- `src/Wiz.Utility/Extensions/JsonExtension.cs`
- `src/Wiz.Utility/Extensions/JsonExtension.Serialization.cs`
- `src/Wiz.Utility/Extensions/JsonExtension.NewtonsoftNaming.cs`

Target framework: `.NET 8.0` (ดู `src/Wiz.Utility/Wiz.Utility.csproj`)

## Prerequisites | ข้อกำหนดเบื้องต้น
- .NET SDK 8.0+
- แพ็กเกจ `Newtonsoft.Json` 13.x (อ้างอิงในโปรเจ็กต์แล้ว)

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
    CreatedUtc = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc),
    Status = OrderStatus.Processing
};

// 1) STJ + camelCase + enum เป็น string + custom date format
string json1 = order.ToJson(
    JsonExtension.JsonEngine.SystemTextJson,
    JsonExtension.JsonCase.CamelCase,
    indented: false,
    dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
    enumAsString: true);

// 2) Deserialize (generic)
Order? back1 = json1.FromJson<Order>(
    JsonExtension.JsonEngine.SystemTextJson,
    JsonExtension.JsonCase.CamelCase,
    dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
    enumAsString: true);

// 3) Newtonsoft + SNAKE_CASE (upper) + indented
string json2 = order.ToJson(
    JsonExtension.JsonEngine.NewtonsoftJson,
    JsonExtension.JsonCase.SnakeCaseUpper,
    indented: true,
    dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
    enumAsString: true);

// 4) Non-generic deserialize
object? obj = json2.FromJson(
    typeof(Order),
    JsonExtension.JsonEngine.NewtonsoftJson,
    JsonExtension.JsonCase.SnakeCaseUpper,
    dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
    enumAsString: true);

// 5) สร้าง options/settings ล่วงหน้า
var stjOptions = JsonExtension.CreateStjOptions(JsonExtension.JsonCase.CamelCase, indented: false, dateFormat: null, enumAsString: true);
var newtonsoftSettings = JsonExtension.CreateNewtonsoftSettings(JsonExtension.JsonCase.KebabCaseLower, indented: false, dateFormat: null, enumAsString: true);
```

ตัวอย่าง Auto-detect naming เมื่ออ่าน:
```csharp
// ตรวจจับจาก JSON keys (camel/snake/kebab/pascal) แล้ว map ให้อัตโนมัติ
Order? autoBack = json1.FromJson<Order>(
    JsonExtension.JsonEngine.SystemTextJson,
    JsonExtension.JsonCase.Auto,
    dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
    enumAsString: true);
```

## API Summary | สรุป API
- Enums
  - `JsonExtension.JsonEngine`
    - `SystemTextJson`, `NewtonsoftJson`
  - `JsonExtension.JsonCase`
    - `Auto`, `PascalCase`, `CamelCase`, `SnakeCaseLower`, `SnakeCaseUpper`, `KebabCaseLower`, `KebabCaseUpper`

- Methods
  - `string ToJson(this object? value, JsonEngine engine = SystemTextJson, JsonCase naming = CamelCase, bool indented = false, string? dateFormat = null, bool enumAsString = true)`
  - `T? FromJson<T>(this string json, JsonEngine engine = SystemTextJson, JsonCase naming = CamelCase, string? dateFormat = null, bool enumAsString = true)`
  - `object? FromJson(this string json, Type returnType, JsonEngine engine = SystemTextJson, JsonCase naming = CamelCase, string? dateFormat = null, bool enumAsString = true)`
  - `JsonSerializerOptions CreateStjOptions(JsonCase naming = CamelCase, bool indented = false, string? dateFormat = null, bool enumAsString = true)`
  - `JsonSerializerSettings CreateNewtonsoftSettings(JsonCase naming = CamelCase, bool indented = false, string? dateFormat = null, bool enumAsString = true)`

## Usage Recipes | ตัวอย่างการใช้งานสำคัญ
- __[Round-trip STJ + camelCase + enum string]__ (จากเทสท์ `RoundTrip_STJ_Camel_EnumAsString`)
  ```csharp
  string json = order.ToJson(JsonExtension.JsonEngine.SystemTextJson, JsonExtension.JsonCase.CamelCase, false, "yyyy-MM-dd'T'HH:mm:ss'Z'", true);
  var back = json.FromJson<Order>(JsonExtension.JsonEngine.SystemTextJson, JsonExtension.JsonCase.CamelCase, "yyyy-MM-dd'T'HH:mm:ss'Z'", true);
  ```

- __[Auto-detect จาก camel/snake/kebab]__ (`JsonCase.Auto`)
  ```csharp
  var back = json.FromJson<Order>(JsonExtension.JsonEngine.SystemTextJson, JsonExtension.JsonCase.Auto, "yyyy-MM-dd'T'HH:mm:ss'Z'", true);
  ```

- __[PascalCase + enum numeric]__ (STJ)
  ```csharp
  string json = order.ToJson(JsonExtension.JsonEngine.SystemTextJson, JsonExtension.JsonCase.PascalCase, false, "yyyy-MM-dd'T'HH:mm:ss'Z'", enumAsString: false);
  // e.g. "Status":2
  ```

- __[Newtonsoft + snake_case lower + indented]__
  ```csharp
  string json = order.ToJson(JsonExtension.JsonEngine.NewtonsoftJson, JsonExtension.JsonCase.SnakeCaseLower, indented: true, dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'", enumAsString: true);
  // keys เช่น "order_id"
  ```

- __[Non-generic FromJson]__
  ```csharp
  object? obj = json.FromJson(typeof(Order), JsonExtension.JsonEngine.NewtonsoftJson, JsonExtension.JsonCase.Auto, dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'", enumAsString: true);
  ```

- __[สร้าง STJ options + date format]__ (ทดสอบ `CreateStjOptions_DateFormat_AppliedOnSerialize`)
  ```csharp
  var opts = JsonExtension.CreateStjOptions(JsonExtension.JsonCase.CamelCase, indented: false, dateFormat: "yyyy/MM/dd HH:mm:ss", enumAsString: true);
  var payload = new { When = new DateTime(2025, 8, 16, 1, 2, 3, DateTimeKind.Utc) };
  var json = System.Text.Json.JsonSerializer.Serialize(payload, opts);
  // "when":"2025/08/16 01:02:03"
  ```

- __[Newtonsoft settings + SNAKE_CASE upper]__
  ```csharp
  var settings = JsonExtension.CreateNewtonsoftSettings(JsonExtension.JsonCase.SnakeCaseUpper, indented: false, dateFormat: "yyyy-MM-dd", enumAsString: true);
  var payload = new { OrderId = "X", CreatedUtc = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc) };
  var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload, settings);
  // คีย์เป็น "ORDER_ID", "CREATED_UTC"
  ```

- __[Enum string case-insensitive เมื่ออ่าน]__ (ทั้ง STJ/Newtonsoft)
  ```csharp
  var back = json.FromJson<Order>(JsonExtension.JsonEngine.SystemTextJson, JsonExtension.JsonCase.CamelCase, dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'", enumAsString: true);
  // "Processing" / "processing" / "PROCESSING" อ่านได้เท่ากัน
  ```

- __[Enum numeric ขณะ enumAsString = true ก็ยังอ่านได้]__
  ```csharp
  var back = json.FromJson<Order>(JsonExtension.JsonEngine.SystemTextJson, JsonExtension.JsonCase.CamelCase, dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'", enumAsString: true);
  // status: 2 หรือ "2" -> map ได้
  ```

- __[Dictionary key naming กับ Newtonsoft]__
  ```csharp
  var dict = new Dictionary<string,int> { ["OrderId"] = 1 };
  string json = dict.ToJson(JsonExtension.JsonEngine.NewtonsoftJson, JsonExtension.JsonCase.KebabCaseUpper, indented: false, dateFormat: null, enumAsString: true);
  // ได้คีย์ "ORDER-ID"
  ```

- __[DateTimeOffset custom format (STJ)]__
  ```csharp
  var opts = JsonExtension.CreateStjOptions(JsonExtension.JsonCase.CamelCase, indented: false, dateFormat: "yyyy-MM-dd HH:mm", enumAsString: true);
  var json = System.Text.Json.JsonSerializer.Serialize(new { When = new DateTimeOffset(2025,08,16,01,02,00, TimeSpan.FromHours(7)) }, opts);
  // "when":"2025-08-16 01:02"
  ```

- __[PropertyNameCaseInsensitive (STJ)]__
  ```csharp
  var back = json.FromJson<Order>(JsonExtension.JsonEngine.SystemTextJson, JsonExtension.JsonCase.CamelCase, dateFormat: null, enumAsString: true);
  // JSON เป็น Pascal ก็ยังแมปได้
  ```

## Compatibility Notes | ความเข้ากันได้
- __STJ__
  - ไม่ยอมรับ trailing comma ตามค่าเริ่มต้น: JSON ที่มี `,` ปลายบรรทัดจะ throw (`FromJson_STJ_TrailingComma_Throws`).
  - มี `PropertyNameCaseInsensitive = true` อ่านคีย์ไม่ตรงเคสได้
  - Snake/Kebab naming policies ต้อง .NET 8+ (`JsonNamingPolicy.SnakeCase* / KebabCase*`)
- __Newtonsoft__
  - ยอมรับ trailing comma โดยปริยาย (`FromJson_Newtonsoft_TrailingComma_Succeeds`)
  - รองรับ dictionary/extension-data key naming ผ่าน `NamingStrategy` และภายในมี `DelimitedCaseNamingStrategy` สำหรับ kebab/snake upper/lower

- __Auto-Detect Naming__
  - `DetectJsonCase(json)` จะสุ่มตัวอย่างคีย์จาก object/array และจัดประเภทเป็น Camel/Pascal/Snake/Kebab
  - หากตรวจจับไม่ได้/JSON ไม่ถูกต้อง -> fallback เป็น `CamelCase`

## Notes & Limitations | หมายเหตุ/ข้อจำกัด
- เมื่อกำหนด `dateFormat`, การเขียนใช้ `InvariantCulture` ส่วนการอ่านพยายาม parse ตาม format ที่กำหนดก่อน แล้ว fallback เป็น ISO/BCL ปกติ (STJ converters ภายใน)
- ค่าของ enum เมื่อ serialize เป็น string จะไม่ถูกเปลี่ยนตาม property naming policy (ทั้ง STJ/Newtonsoft)
- Null values ถูกเขียนออก (ตัวอย่าง: `"orderId":null` ใน STJ)

## Security | ความปลอดภัย
- ไม่มีการเก็บ secrets
- ควร validate/จำกัด input JSON จากภายนอกเสมอ

## References | อ้างอิงโค้ด/เทสท์
- โค้ด: `src/Wiz.Utility/Extensions/JsonExtension.Serialization.cs`, `JsonExtension.NewtonsoftNaming.cs`
- เทสท์: `tests/Wiz.Utility.Test/Extensions/JsonExtensionTests.cs`
