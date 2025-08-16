# NumericExtensions

ส่วนขยาย (extension methods) สำหรับงานตัวเลขที่พบบ่อย ครอบคลุม `int`, `long`, `double`, และ `decimal` อยู่ในเนมสเปซ `Wiz.Utility.Extensions`.

- ใช้งานโดย `using Wiz.Utility.Extensions;`
- ออกแบบให้ปลอดภัยและคาดเดาได้: รองรับกรณีค่าติดลบ, การสลับ min/max, ค่า NaN/Infinity (สำหรับ `double`), และการปัดเศษที่ควบคุมได้

## Installation & Usage
เพิ่มการอ้างอิงโปรเจ็กต์ `Wiz.Utility`, จากนั้นนำเข้าเนมสเปซ

```csharp
using Wiz.Utility.Extensions;
```

## API Overview
- Even/Odd
  - `bool IsEven(this int value)`
  - `bool IsOdd(this int value)`
  - `bool IsEven(this long value)`
  - `bool IsOdd(this long value)`
- Range
  - `bool IsBetween(this int|long|double|decimal value, min, max, bool inclusive = true)`
  - `T Clamp(this T value, T min, T max)` สำหรับ `int|long|double|decimal`
- Approximate equality
  - `bool NearlyEquals(this double a, double b, double epsilon = 1e-10, double? relativeTolerance = null)`
  - `bool NearlyEquals(this decimal a, decimal b, decimal epsilon = 0.0000001m)`
- Mapping
  - `double MapRange(this double value, double sourceMin, double sourceMax, double targetMin, double targetMax, bool clamp = false)`
  - `decimal MapRange(this decimal value, decimal sourceMin, decimal sourceMax, decimal targetMin, decimal targetMax, bool clamp = false)`
- Ordinal (English)
  - `string ToOrdinal(this int value, CultureInfo? culture = null)`
  - `string ToOrdinal(this long value, CultureInfo? culture = null)`
- Human-readable size (bytes)
  - `string ToHumanSize(this long bytes, int decimals = 1, bool binary = false, CultureInfo? culture = null)`
  - Overloads: `int`, `double`, `decimal`
- Rounding & Safe divide
  - `decimal RoundTo(this decimal value, int decimals, MidpointRounding mode = MidpointRounding.AwayFromZero)`
  - `double SafeDivide(this double numerator, double denominator, double defaultValue = 0.0)`
  - `decimal SafeDivide(this decimal numerator, decimal denominator, decimal defaultValue = 0m)`

## Behavior Details
- IsEven/IsOdd: ใช้ bitwise เช็กเลขคู่/คี่ รองรับค่าติดลบและ `long` อย่างมีประสิทธิภาพ
- IsBetween: หาก `min > max` จะสลับให้อัตโนมัติ; `inclusive` กำหนดรวมปลายช่วงหรือไม่
- Clamp: หาก `min > max` จะสลับให้อัตโนมัติ; ตัดค่ากลับสู่ช่วง [min, max]
- NearlyEquals (double):
  - หากมี `relativeTolerance` (> 0) จะคิด tolerance = max(epsilon, relativeTolerance * max(|a|, |b|))
  - จัดการ `NaN` (ให้ผล `false`) และ `Infinity` (เท่ากันต้องชนิดเดียวกัน)
- NearlyEquals (decimal): ใช้ `epsilon` แบบสัมบูรณ์
- MapRange: แปลงค่าจากช่วงหนึ่งไปยังอีกช่วงหนึ่ง; ถ้า `clamp = true` จะหนีบค่าให้อยู่ใน source ช่วงก่อนคำนวณ; โยน `ArgumentException` หาก `sourceMin == sourceMax`
- ToOrdinal: คืน suffix ภาษาอังกฤษ (`st`, `nd`, `rd`, `th`) รองรับค่าติดลบและกรณีพิเศษ 11/12/13
- ToHumanSize:
  - `binary = false` ใช้หน่วยฐาน 1000: `B, KB, MB, GB, TB, PB, EB`
  - `binary = true` ใช้หน่วยฐาน 1024: `B, KiB, MiB, GiB, TiB, PiB, EiB`
  - `decimals` ควบคุมจำนวนตำแหน่งทศนิยม (ปัด `AwayFromZero` เมื่อ `decimals = 0`)
  - รองรับค่าติดลบและ `CultureInfo`
- RoundTo: ปัด `decimal` ด้วยโหมดที่กำหนด (เริ่มต้น `AwayFromZero`)
- SafeDivide: ถ้าส่วนเป็นศูนย์ คืน `defaultValue` เพื่อกัน exception

## Quick Examples
ตัวอย่างสั้นๆ ที่ครอบคลุมเคสหลักและ edge cases

```csharp
using System;
using System.Globalization;
using Wiz.Utility.Extensions;

class Demo
{
    static void Main()
    {
        // Even / Odd
        Console.WriteLine(0.IsEven());      // True
        Console.WriteLine(5L.IsOdd());      // True

        // IsBetween (inclusive by default) + swapped bounds
        Console.WriteLine(5.IsBetween(1, 5));           // True
        Console.WriteLine(5.IsBetween(1, 5, false));    // False
        Console.WriteLine(5.IsBetween(10, 1));          // True (auto-swap)

        // Clamp
        Console.WriteLine(0.Clamp(1, 10));  // 1
        Console.WriteLine(11.Clamp(1, 10)); // 10

        // NearlyEquals (double + relative tolerance)
        double a = 0.1 + 0.2; // 0.30000000000000004
        Console.WriteLine(a.NearlyEquals(0.3, epsilon: 1e-9)); // True
        Console.WriteLine(1000.0.NearlyEquals(1000.1, epsilon: 1e-12, relativeTolerance: 1e-4)); // True

        // NearlyEquals (decimal)
        Console.WriteLine(1.0000000m.NearlyEquals(1.00000005m, 0.000001m)); // True

        // MapRange (with clamp)
        Console.WriteLine(5d.MapRange(0, 10, 0, 100)); // 50
        Console.WriteLine((-2d).MapRange(0, 10, 0, 100, clamp: true)); // 0

        // Ordinal (English)
        Console.WriteLine(1.ToOrdinal());   // 1st
        Console.WriteLine((-1).ToOrdinal()); // -1st
        Console.WriteLine(11.ToOrdinal());  // 11th

        // Human size (decimal vs binary)
        Console.WriteLine(1536L.ToHumanSize());            // 1.5 KB (base 1000)
        Console.WriteLine(1536L.ToHumanSize(binary: true)); // 1.5 KiB (base 1024)

        // Culture
        var th = new CultureInfo("th-TH");
        Console.WriteLine(1048576L.ToHumanSize(culture: th)); // 1.0 MB (using Thai number formatting)

        // RoundTo & SafeDivide
        Console.WriteLine(1.2345m.RoundTo(2)); // 1.23
        Console.WriteLine(10.0.SafeDivide(0.0, defaultValue: -1.0)); // -1
    }
}
```

## Advanced Usage & Notes
- Relative tolerance (`NearlyEquals` for `double`): เลือกใช้เมื่อค่ามีขนาดใหญ่/เล็กมาก เพื่อให้ tolerance ปรับตามสเกลของค่าที่เปรียบเทียบ
- Mapping นอกช่วง: ตั้ง `clamp = true` เพื่อป้องกันค่าเกิน target ช่วงโดยไม่ต้องเช็กก่อน
- Human size:
  - เลือก `binary: true` เมื่อสื่อสารกับระบบไฟล์/หน่วยความจำซึ่งใช้ฐาน 1024
  - ปรับ `decimals` เพื่อลด/เพิ่มความละเอียดของการแสดงผล
- Localization: เมธอดที่รับ `CultureInfo` (`ToOrdinal`, `ToHumanSize`) รองรับการจัดรูปแบบตัวเลขตามภาษา/ท้องถิ่น (suffix ของ `ToOrdinal` เป็นภาษาอังกฤษเสมอ)

## Tested Behavior
มีชุดทดสอบใน `tests/Wiz.Utility.Test/Extensions/NumericExtensionsTests.cs` ครอบคลุม:
- Even/Odd, IsBetween (inclusive/exclusive + swapped bounds)
- Clamp (ทุกชนิดข้อมูล)
- NearlyEquals (double/decimal, epsilon/relative tolerance)
- MapRange (with clamp)
- ToOrdinal (เคส 11/12/13, ค่าติดลบ)
- ToHumanSize (decimal/binary units, overloads)
- RoundTo และ SafeDivide
