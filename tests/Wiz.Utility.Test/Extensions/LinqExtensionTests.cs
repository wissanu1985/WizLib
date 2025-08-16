using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Wiz.Utility.Extensions;
using Xunit;

namespace Wiz.Utility.Test.Extensions
{
    public class LinqExtensionTests
    {
        private sealed class RoColl<T> : IReadOnlyCollection<T>
        {
            private readonly List<T> _inner;
            public RoColl(IEnumerable<T> items) => _inner = new List<T>(items);
            public int Count => _inner.Count;
            public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _inner.GetEnumerator();
        }

        private sealed class AsyncRoColl<T> : IAsyncEnumerable<T>, IReadOnlyCollection<T>
        {
            private readonly List<T> _inner;
            public AsyncRoColl(IEnumerable<T> items) => _inner = new List<T>(items);
            public int Count => _inner.Count;
            public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _inner.GetEnumerator();
            public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                foreach (var item in _inner)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return item;
                    await Task.Yield();
                }
            }
        }

        private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> src, [EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var item in src)
            {
                ct.ThrowIfCancellationRequested();
                yield return item;
                await Task.Yield();
            }
        }

        [Fact]
        public void TryFirst_IReadOnlyList_UsesFastPath()
        {
            IReadOnlyList<int> ro = new List<int> { 10, 20, 30 };
            ro.TryFirst(out var v).ShouldBeTrue();
            v.ShouldBe(10);
        }

        [Fact]
        public void TrySingle_IReadOnlyList_FastPath_SingleAndNotSingle()
        {
            IReadOnlyList<string> one = new List<string> { "only" };
            one.TrySingle(out var v1).ShouldBeTrue();
            v1.ShouldBe("only");

            IReadOnlyList<string> many = new List<string> { "a", "b" };
            many.TrySingle(out var v2).ShouldBeFalse();
            v2.ShouldBe(default);
        }

        [Fact]
        public void TryFirst_Null_Throws()
        {
            IEnumerable<int>? src = null;
            Should.Throw<ArgumentNullException>(() => src!.TryFirst(out _));
        }

        [Fact]
        public void TryFirst_Empty_ReturnsFalse_DefaultOut()
        {
            var ok = Array.Empty<int>().TryFirst(out var v);
            ok.ShouldBeFalse();
            v.ShouldBe(default);
        }

        [Fact]
        public void TryFirst_List_FastPath_ReturnsFirst()
        {
            var ok = new List<string> { "a", "b" }.TryFirst(out var v);
            ok.ShouldBeTrue();
            v.ShouldBe("a");
        }

        [Fact]
        public void TrySingle_Null_Throws()
        {
            IEnumerable<int>? src = null;
            Should.Throw<ArgumentNullException>(() => src!.TrySingle(out _));
        }

        [Fact]
        public void TrySingle_EmptyOrMany_ReturnsFalse()
        {
            Array.Empty<int>().TrySingle(out var v1).ShouldBeFalse();
            v1.ShouldBe(default);

            new[] { 1, 2 }.TrySingle(out var v2).ShouldBeFalse();
            v2.ShouldBe(default);
        }

        [Fact]
        public void TrySingle_Single_ReturnsTrueValue()
        {
            new[] { 42 }.TrySingle(out var v).ShouldBeTrue();
            v.ShouldBe(42);
        }

        [Fact]
        public void ToArrayFast_Null_Throws()
        {
            IEnumerable<int>? src = null;
            Should.Throw<ArgumentNullException>(() => src!.ToArrayFast());
        }

        [Fact]
        public void ToArrayFast_ICollection_ExactCopy_NoExtra()
        {
            var list = Enumerable.Range(1, 5).ToList();
            var arr = list.ToArrayFast();
            arr.ShouldBe(new[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public void ToArrayFast_Iterator_FallbackMatchesLinq()
        {
            IEnumerable<int> Gen()
            {
                for (int i = 0; i < 10; i++) yield return i * i;
            }
            var expected = Gen().ToArray();
            var actual = Gen().ToArrayFast();
            actual.ShouldBe(expected);
        }

        [Fact]
        public void ToListFast_ICollection_PreallocAndCopy()
        {
            var src = new HashSet<string> { "x", "y", "z" };
            var list = src.ToListFast();
            list.Count.ShouldBe(3);
            list.OrderBy(s => s).ShouldBe(src.OrderBy(s => s));
        }

        [Fact]
        public void SelectToList_NullArgs_Throw()
        {
            IEnumerable<int>? src = null;
            Should.Throw<ArgumentNullException>(() => src!.SelectToList(i => i));
            Should.Throw<ArgumentNullException>(() => new[] { 1 }.SelectToList<int, int>(null!));
        }

        [Fact]
        public void SelectToList_ProjectsSinglePass_EqualsLinq()
        {
            var src = Enumerable.Range(1, 100);
            var expected = src.Select(i => i * 2).ToList();
            var actual = src.SelectToList(i => i * 2);
            actual.ShouldBe(expected);
        }

        [Fact]
        public void WhereSelectToList_NullArgs_Throw()
        {
            IEnumerable<int>? src = null;
            Should.Throw<ArgumentNullException>(() => src!.WhereSelectToList(i => true, i => i));
            Should.Throw<ArgumentNullException>(() => new[] { 1 }.WhereSelectToList<int, int>(null!, i => i));
            Should.Throw<ArgumentNullException>(() => new[] { 1 }.WhereSelectToList<int, int>(i => true, null!));
        }

        [Fact]
        public void WhereSelectToList_FilterAndProject_EqualsLinq()
        {
            var src = Enumerable.Range(0, 1000);
            var expected = src.Where(i => (i & 1) == 0).Select(i => i * i).ToList();
            var actual = src.WhereSelectToList(i => (i & 1) == 0, i => i * i);
            actual.ShouldBe(expected);
        }

        [Fact(Skip = "Performance smoke test; skip in CI")]
        [Trait("Category", "Benchmark")]
        public void WhereSelectToList_Perf_Smoke()
        {
            var src = Enumerable.Range(0, 1_000_00);
            // Ensure no huge slow-down vs LINQ chain
            var t1 = System.Diagnostics.Stopwatch.StartNew();
            var a = src.WhereSelectToList(i => (i & 1) == 0, i => i + 1);
            t1.Stop();

            var t2 = System.Diagnostics.Stopwatch.StartNew();
            var b = src.Where(i => (i & 1) == 0).Select(i => i + 1).ToList();
            t2.Stop();

            a.Count.ShouldBe(b.Count);
            // Fast path should be at least not significantly slower
            (t1.Elapsed.TotalMilliseconds <= t2.Elapsed.TotalMilliseconds * 1.5).ShouldBeTrue();
        }

        [Fact]
        public void ForEachFast_Action_Null_Throws()
        {
            Should.Throw<ArgumentNullException>(() => new[] { 1 }.ForEachFast((Action<int>)null!));
        }

        [Fact]
        public void ForEachFast_Simple_CountsItems()
        {
            int count = 0;
            Enumerable.Range(1, 10).ForEachFast(_ => count++);
            count.ShouldBe(10);
        }

        [Fact]
        public void ForEachFast_WithIndex_ValidatesSequence()
        {
            var items = new[] { "a", "b", "c" };
            var acc = new List<string>();
            items.ForEachFast((item, idx) => acc.Add($"{idx}:{item}"));
            acc.ShouldBe(new[] { "0:a", "1:b", "2:c" });
        }

        // Async variants
        [Fact]
        public async Task TryFirstAsync_Empty_ReturnsFalse()
        {
            var src = ToAsync(Array.Empty<int>());
            var (ok, value) = await src.TryFirstAsync();
            ok.ShouldBeFalse();
            value.ShouldBe(default);
        }

        [Fact]
        public async Task TryFirstAsync_HasItem_ReturnsTrueValue()
        {
            var src = ToAsync(new[] { 7, 8, 9 });
            var (ok, value) = await src.TryFirstAsync();
            ok.ShouldBeTrue();
            value.ShouldBe(7);
        }

        [Fact]
        public async Task TrySingleAsync_Single_ReturnsTrueValue()
        {
            var src = ToAsync(new[] { "x" });
            var (ok, value) = await src.TrySingleAsync();
            ok.ShouldBeTrue();
            value.ShouldBe("x");
        }

        [Fact]
        public async Task TrySingleAsync_EmptyOrMany_ReturnsFalse()
        {
            var none = ToAsync(Array.Empty<int>());
            var many = ToAsync(new[] { 1, 2 });

            (await none.TrySingleAsync()).Success.ShouldBeFalse();
            (await many.TrySingleAsync()).Success.ShouldBeFalse();
        }

        [Fact]
        public async Task SelectToListAsync_Projects_EqualsSync()
        {
            var src = Enumerable.Range(1, 50);
            var expected = src.Select(i => i * 3).ToList();
            var actual = await ToAsync(src).SelectToListAsync(i => i * 3);
            actual.ShouldBe(expected);
        }

        [Fact]
        public async Task WhereSelectToListAsync_FilterAndProject_EqualsSync()
        {
            var src = Enumerable.Range(0, 200);
            var expected = src.Where(i => (i % 5) == 0).Select(i => i + 1).ToList();
            var actual = await ToAsync(src).WhereSelectToListAsync(i => (i % 5) == 0, i => i + 1);
            actual.ShouldBe(expected);
        }

        [Fact]
        public async Task Async_NullArgs_Throw()
        {
            IAsyncEnumerable<int> src = ToAsync(new[] { 1, 2, 3 });
            await Should.ThrowAsync<ArgumentNullException>(async () => await ((IAsyncEnumerable<int>)null!).SelectToListAsync(i => i));
            await Should.ThrowAsync<ArgumentNullException>(async () => await src.SelectToListAsync<int, int>(null!));
            await Should.ThrowAsync<ArgumentNullException>(async () => await ((IAsyncEnumerable<int>)null!).WhereSelectToListAsync(i => true, i => i));
            await Should.ThrowAsync<ArgumentNullException>(async () => await src.WhereSelectToListAsync<int, int>(null!, i => i));
            await Should.ThrowAsync<ArgumentNullException>(async () => await src.WhereSelectToListAsync<int, int>(i => true, null!));
        }

        [Fact]
        public async Task TryFirstAsync_Null_Throws()
        {
            await Should.ThrowAsync<ArgumentNullException>(async () => await ((IAsyncEnumerable<int>)null!).TryFirstAsync());
        }

        [Fact]
        public async Task TrySingleAsync_Null_Throws()
        {
            await Should.ThrowAsync<ArgumentNullException>(async () => await ((IAsyncEnumerable<int>)null!).TrySingleAsync());
        }

        [Fact]
        public void ForEachFast_Source_Null_Throws()
        {
            IEnumerable<int>? src = null;
            Should.Throw<ArgumentNullException>(() => src!.ForEachFast(_ => { }));
            Should.Throw<ArgumentNullException>(() => src!.ForEachFast((int _, int __) => { }));
        }

        [Fact]
        public async Task TrySingleAsync_Cancellation_Throws()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var src = ToAsync(new[] { 1 }, cts.Token);
            await Should.ThrowAsync<OperationCanceledException>(async () => await src.TrySingleAsync(cts.Token));
        }

        [Fact]
        public void ForEachFast_WithIndex_Action_Null_Throws()
        {
            Should.Throw<ArgumentNullException>(() => new[] { 1, 2 }.ForEachFast((Action<int, int>)null!));
        }

        [Fact]
        public void ToListFast_Iterator_FallbackMatchesLinq()
        {
            IEnumerable<int> Gen()
            {
                for (int i = 0; i < 17; i++) yield return i - 3;
            }
            var expected = Gen().ToList();
            var actual = Gen().ToListFast();
            actual.ShouldBe(expected);
        }

        [Fact]
        public void SelectToList_KnownCount_Prealloc_EqualsLinq()
        {
            var src = new HashSet<int>(Enumerable.Range(1, 20));
            var expected = src.Select(i => i + 10).OrderBy(i => i).ToList();
            var actual = src.SelectToList(i => i + 10).OrderBy(i => i).ToList();
            actual.ShouldBe(expected);
        }

        [Fact]
        public async Task TryFirstAsync_Cancellation_Throws()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var src = ToAsync(Enumerable.Range(1, 3), cts.Token);
            await Should.ThrowAsync<OperationCanceledException>(async () => await src.TryFirstAsync(cts.Token));
        }

        [Fact]
        public async Task WhereSelectToListAsync_Cancellation_Throws()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var src = ToAsync(Enumerable.Range(0, 100), cts.Token);
            await Should.ThrowAsync<OperationCanceledException>(async () => await src.WhereSelectToListAsync(i => (i & 1) == 0, i => i + 1, cts.Token));
        }

        [Fact]
        public void TryFirst_Iterator_ReturnsFirst()
        {
            IEnumerable<int> Gen()
            {
                yield return 5;
                yield return 6;
            }

            var ok = Gen().TryFirst(out var v);
            ok.ShouldBeTrue();
            v.ShouldBe(5);
        }

        [Fact]
        public void TrySingle_Iterator_SingleAndMany()
        {
            IEnumerable<string> One()
            {
                yield return "only";
            }
            IEnumerable<string> Many()
            {
                yield return "a";
                yield return "b";
            }

            One().TrySingle(out var s1).ShouldBeTrue();
            s1.ShouldBe("only");

            Many().TrySingle(out var s2).ShouldBeFalse();
            s2.ShouldBe(default);
        }

        [Fact]
        public void WhereSelectToList_TrimExcess_Branch_Executed()
        {
            // Capacity hint = 100; predicate matches 1 -> triggers TrimExcess path
            var src = Enumerable.Range(0, 100).ToList();
            var result = src.WhereSelectToList(i => i == 42, i => i * 2);
            result.Count.ShouldBe(1);
            result[0].ShouldBe(84);
        }

        [Fact]
        public void ToArrayFast_IReadOnlyCollection_Path()
        {
            var src = new RoColl<int>(Enumerable.Range(1, 7));
            var arr = src.ToArrayFast();
            arr.ShouldBe(new[] { 1, 2, 3, 4, 5, 6, 7 });
        }

        [Fact]
        public void ToArrayFast_Empty_ICollection_ReturnsEmpty()
        {
            var src = new List<int>();
            var arr = src.ToArrayFast();
            arr.ShouldBe(Array.Empty<int>());
        }

        [Fact]
        public void ToArrayFast_Empty_IReadOnlyCollection_ReturnsEmpty()
        {
            var src = new RoColl<int>(Array.Empty<int>());
            var arr = src.ToArrayFast();
            arr.ShouldBe(Array.Empty<int>());
        }

        [Fact]
        public void ToListFast_IReadOnlyCollection_Path()
        {
            var src = new RoColl<string>(new[] { "p", "q", "r" });
            var list = src.ToListFast();
            list.ShouldBe(new[] { "p", "q", "r" });
        }

        [Fact]
        public void ToListFast_Empty_ICollection_ReturnsEmpty()
        {
            var src = new HashSet<string>();
            var list = src.ToListFast();
            list.Count.ShouldBe(0);
        }

        [Fact]
        public void ToListFast_Empty_IReadOnlyCollection_ReturnsEmpty()
        {
            var src = new RoColl<int>(Array.Empty<int>());
            var list = src.ToListFast();
            list.Count.ShouldBe(0);
        }

        [Fact]
        public void SelectToList_IReadOnlyCollection_Path()
        {
            var src = new RoColl<int>(Enumerable.Range(0, 5));
            var list = src.SelectToList(i => i + 10);
            list.ShouldBe(new[] { 10, 11, 12, 13, 14 });
        }

        [Fact]
        public void SelectToList_Empty_ICollection_ReturnsEmpty()
        {
            var src = new List<int>();
            var list = src.SelectToList(i => i + 1);
            list.Count.ShouldBe(0);
        }

        [Fact]
        public void SelectToList_Empty_IReadOnlyCollection_ReturnsEmpty()
        {
            var src = new RoColl<int>(Array.Empty<int>());
            var list = src.SelectToList(i => i + 1);
            list.Count.ShouldBe(0);
        }

        [Fact]
        public void WhereSelectToList_IReadOnlyCollection_Path_WithTrim()
        {
            var src = new RoColl<int>(Enumerable.Range(0, 50));
            var list = src.WhereSelectToList(i => i == 0 || i == 49, i => i);
            list.ShouldBe(new[] { 0, 49 });
        }

        [Fact]
        public async Task SelectToListAsync_IReadOnlyCollection_CapacityHint_Path()
        {
            var src = new AsyncRoColl<int>(Enumerable.Range(1, 4));
            var list = await src.SelectToListAsync(i => i * 3);
            list.ShouldBe(new[] { 3, 6, 9, 12 });
        }

        [Fact]
        public async Task WhereSelectToListAsync_IReadOnlyCollection_CapacityAndTrim_Path()
        {
            var src = new AsyncRoColl<int>(Enumerable.Range(0, 40));
            var list = await src.WhereSelectToListAsync(i => i == 10, i => i + 1);
            list.ShouldBe(new[] { 11 });
        }
    }
}
