# LinqExtension — High-performance LINQ helpers

Primary prose: Thai. Code/identifiers: English.

## ภาพรวม (Overview)
`Wiz.Utility.Extensions.LinqExtension` คือชุด extension methods สำหรับ `IEnumerable<T>` และ `IAsyncEnumerable<T>` ที่มุ่งลด allocation และหลีกเลี่ยงการทำงานหลายรอบโดยไม่จำเป็น โดยมี fast-path สำหรับ collection ที่ทราบจำนวนล่วงหน้า (`ICollection<T>`, `IReadOnlyCollection<T>`, `IList<T>`, `IReadOnlyList<T>`) และมีเวอร์ชัน async ที่รองรับ `CancellationToken` ตามแนวทาง .NET

คุณสมบัติเด่น:
- ลดการโยน Exception จาก pattern พวก `First/Single` ด้วย `TryFirst/TrySingle` (sync/async) ที่คืนค่าแบบ tuple หรือ out parameter
- Materialize อย่างมีประสิทธิภาพ: `ToArrayFast`, `ToListFast`
- One-pass projection/filter + materialize: `SelectToList`, `WhereSelectToList` (และเวอร์ชัน async)
- Utility เดินรายการแบบเบา ๆ: `ForEachFast` (มี overload แบบรับ index)

Namespace: `Wiz.Utility.Extensions`

---

## การติดตั้งใช้งาน (Usage)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wiz.Utility.Extensions;

public class Demo
{
    public static async Task RunAsync()
    {
        // TryFirst/TrySingle (sync)
        var numbers = new List<int> { 10, 20, 30 };
        if (numbers.TryFirst(out var first))
        {
            Console.WriteLine($"first = {first}"); // 10
        }
        if (new[] { 42 }.TrySingle(out var only))
        {
            Console.WriteLine($"only = {only}"); // 42
        }

        // ToArrayFast/ToListFast
        var arr = numbers.ToArrayFast();
        var list = new HashSet<string> { "x", "y", "z" }.ToListFast();

        // SelectToList / WhereSelectToList
        var doubled = Enumerable.Range(1, 5).SelectToList(i => i * 2); // [2,4,6,8,10]
        var squaresOfEven = Enumerable.Range(0, 10)
            .WhereSelectToList(i => (i & 1) == 0, i => i * i); // [0,4,16,36,64]

        // ForEachFast
        var acc = new List<string>();
        new[] { "a", "b", "c" }.ForEachFast((item, idx) => acc.Add($"{idx}:{item}"));
        // acc = ["0:a", "1:b", "2:c"]

        // Async source helper (example)
        static async IAsyncEnumerable<int> ToAsync(IEnumerable<int> src, CancellationToken ct = default)
        {
            foreach (var item in src)
            {
                ct.ThrowIfCancellationRequested();
                yield return item;
                await Task.Yield();
            }
        }

        // TryFirstAsync/TrySingleAsync
        var (ok1, firstAsync) = await ToAsync(new[] { 7, 8, 9 }).TryFirstAsync();
        var (ok2, singleAsync) = await ToAsync(new[] { 100 }).TrySingleAsync();

        // SelectToListAsync / WhereSelectToListAsync
        var tripled = await ToAsync(Enumerable.Range(1, 4)).SelectToListAsync(i => i * 3); // [3,6,9,12]
        var filtered = await ToAsync(Enumerable.Range(0, 8))
            .WhereSelectToListAsync(i => (i % 3) == 0, i => i + 1); // [1,4,7]
    }
}
```

---

## รายการ API แบบย่อ (API at a glance)

- Try family
  - `bool TryFirst<T>(this IEnumerable<T> source, out T value)`
  - `bool TryFirst<T>(this IReadOnlyList<T> source, out T value)`
  - `ValueTask<(bool Success, T Value)> TryFirstAsync<T>(this IAsyncEnumerable<T> source, CancellationToken ct = default)`
  - `bool TrySingle<T>(this IEnumerable<T> source, out T value)`
  - `bool TrySingle<T>(this IReadOnlyList<T> source, out T value)`
  - `ValueTask<(bool Success, T Value)> TrySingleAsync<T>(this IAsyncEnumerable<T> source, CancellationToken ct = default)`

- Materialization
  - `T[] ToArrayFast<T>(this IEnumerable<T> source)`
  - `List<T> ToListFast<T>(this IEnumerable<T> source)`

- One-pass projection/filter + materialize
  - `List<TResult> SelectToList<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector)`
  - `List<TResult> WhereSelectToList<T, TResult>(this IEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector)`
  - `ValueTask<List<TResult>> SelectToListAsync<T, TResult>(this IAsyncEnumerable<T> source, Func<T, TResult> selector, CancellationToken ct = default)`
  - `ValueTask<List<TResult>> WhereSelectToListAsync<T, TResult>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector, CancellationToken ct = default)`

- Iteration helpers
  - `void ForEachFast<T>(this IEnumerable<T> source, Action<T> action)`
  - `void ForEachFast<T>(this IEnumerable<T> source, Action<T, int> action)`

อ้างอิงโค้ดจริง: `src/Wiz.Utility/Extensions/LinqExtension.cs`
ชุดทดสอบตัวอย่าง: `tests/Wiz.Utility.Test/Extensions/LinqExtensionTests.cs`

---

## รายละเอียดพฤติกรรมสำคัญ (Behavior & Notes)

- การป้องกัน null:
  - ทุกเมธอดตรวจสอบ `source`/delegate เป็น `null` แล้ว `throw ArgumentNullException` ตามรูปแบบในโค้ดจริง
- ผลลัพธ์ `TryFirst/TrySingle`:
  - `TryFirst` คืน `false` พร้อม `default` เมื่อว่างเปล่า; ไม่มีการโยนข้อยกเว้น
  - `TrySingle` คืน `false` เมื่อไม่มีหรือมีมากกว่า 1; ไม่มีการโยนข้อยกเว้น
  - เวอร์ชัน `Async` คืน `(Success, Value)` เช่นเดียวกัน
- Fast path / Capacity hint:
  - หาก `source` เป็น `ICollection<T>`/`IReadOnlyCollection<T>` จะ preallocate ขนาดที่เหมาะสมทันที (เช่น `ToArrayFast`, `ToListFast`, `SelectToList`, `WhereSelectToList`)
  - สำหรับ `WhereSelectToList(…)` มีการ `TrimExcess()` เมื่อ over-allocate มาก (เช่น เติมได้น้อยกว่าครึ่ง)
- Async + Cancellation:
  - เวอร์ชัน async ใช้ `await foreach (source.WithCancellation(ct))` ตามมาตรฐาน .NET
  - หาก `ct` ถูกยกเลิก การ enumerate/เมธอดจะโยน `OperationCanceledException`
- ความสอดคล้องกับ LINQ ปกติ:
  - เส้นทาง fallback ใช้ `Enumerable.ToArray/ToList` เพื่อให้ผลลัพธ์สอดคล้องกับ LINQ พื้นฐาน

---

## แนวทางการเลือกใช้งาน (When to use)

- ใช้ `TryFirst/TrySingle` เมื่อไม่ต้องการ exceptions จาก `First/Single` และอยากได้เส้นทางเร็วบน list
- ใช้ `ToArrayFast/ToListFast` เมื่อ materialize จาก collection ที่ทราบจำนวน เพื่อหลีกเลี่ยง re-allocation
- ใช้ `SelectToList/WhereSelectToList` เมื่ออยากทำ projection/filter + materialize ในรอบเดียว พร้อม capacity hint
- ใช้เวอร์ชัน async เมื่อแหล่งข้อมูลเป็น `IAsyncEnumerable<T>` และต้องรองรับ `CancellationToken`

---

## ตัวอย่างเชิงลึก (Advanced Examples)

- Fast path กับ `IReadOnlyList<T>`:
```csharp
IReadOnlyList<int> ro = new List<int> { 10, 20, 30 };
ro.TryFirst(out var v).ShouldBeTrue(); // true, v = 10
```

- การ trim เมื่อ over-allocate (สะท้อนจาก test):
```csharp
var src = Enumerable.Range(0, 100).ToList();
var result = src.WhereSelectToList(i => i == 42, i => i * 2);
// capacity hint = 100 แต่เข้าเงื่อนไขเพียง 1 => ภายในจะ TrimExcess บางกรณี
```

- เวอร์ชัน async พร้อมยกเลิก:
```csharp
using var cts = new CancellationTokenSource();
cts.Cancel();
var src = ToAsync(new[] { 1 }, cts.Token);
await Should.ThrowAsync<OperationCanceledException>(async () => await src.TrySingleAsync(cts.Token));
```

---

## ข้อควรระวัง (Caveats)

- Delegate `selector/predicate/action` ที่เป็น `null` จะถูกปฏิเสธด้วย `ArgumentNullException`
- `TrySingle` ไม่โยนเมื่อมากกว่า 1 รายการ แต่จะคืน `false`; หากต้องการข้อยกเว้น ให้ใช้ `Single/SingleOrDefault` ของ LINQ
- สำหรับชุดข้อมูลขนาดเล็ก ความแตกต่างด้านประสิทธิภาพอาจไม่เด่นชัดนัก แต่โค้ดนี้ถูกออกแบบให้ scale ได้ดีและลด allocation เมื่อจำนวนสมาชิกมาก

---

## การทดสอบที่ครอบคลุม (Tests Reference)
ตัวอย่างพฤติกรรมและการใช้งานดูได้จาก:
- `tests/Wiz.Utility.Test/Extensions/LinqExtensionTests.cs`
  - ครอบคลุมทั้งเส้นทาง fast-path, fallback, กรณีว่าง/หลายรายการ, และเวอร์ชัน async พร้อม cancellation

---

## เวอร์ชันและความเข้ากันได้ (Compatibility)
- ออกแบบตามมาตรฐาน .NET 6+ ที่รองรับ `IAsyncEnumerable<T>`
- ใช้งานได้กับ collection interface ใน BCL โดยไม่ต้องพึ่งพา third-party library
