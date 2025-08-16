using NSubstitute;
using System;
using Wiz.Utility.Extensions;
using Xunit;
using Shouldly;
using System.Text.Json;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Wiz.Utility.Test.Extensions
{
    public class JsonExtensionTests
    {
        private enum OrderStatus { New, Processing, Done }

        private sealed class Order
        {
            public string? OrderId { get; set; }
            public DateTime CreatedUtc { get; set; }
            public OrderStatus Status { get; set; }
        }

        [Fact]
        public void RoundTrip_STJ_Camel_EnumAsString()
        {
            // Arrange
            var order = new Order
            {
                OrderId = "A-001",
                CreatedUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = OrderStatus.Processing
            };

            // Act
            string json = order.ToJson(
                JsonExtension.JsonEngine.SystemTextJson,
                JsonExtension.JsonCase.CamelCase,
                indented: false,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: true);

            var back = json.FromJson<Order>(
                JsonExtension.JsonEngine.SystemTextJson,
                JsonExtension.JsonCase.CamelCase,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: true);

            // Assert
            back.ShouldNotBeNull();
            back!.OrderId.ShouldBe(order.OrderId);
            back.Status.ShouldBe(order.Status);
            json.ShouldContain("\"orderId\"");
            json.ShouldContain("\"status\":\"Processing\"");
        }

        [Fact]
        public void AutoDetect_Camel_FromJson()
        {
            // Arrange: camelCase JSON
            var json = "{\"orderId\":\"A-001\",\"createdUtc\":\"2025-01-01T00:00:00Z\",\"status\":\"Processing\"}";

            // Act
            var back = json.FromJson<Order>(
                JsonExtension.JsonEngine.SystemTextJson,
                JsonExtension.JsonCase.Auto,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: true);

            // Assert
            back.ShouldNotBeNull();
            back!.OrderId.ShouldBe("A-001");
            back.Status.ShouldBe(OrderStatus.Processing);
        }

        [Fact]
        public void ToJson_STJ_Pascal_EnumAsNumber()
        {
            // Arrange
            var order = new Order
            {
                OrderId = "B-002",
                CreatedUtc = new DateTime(2025, 6, 1, 12, 30, 0, DateTimeKind.Utc),
                Status = OrderStatus.Done
            };

            // Act
            string json = order.ToJson(
                JsonExtension.JsonEngine.SystemTextJson,
                JsonExtension.JsonCase.PascalCase,
                indented: false,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: false);

            // Assert: PascalCase keeps property names; enum numeric ("2")
            json.ShouldContain("\"OrderId\"");
            json.ShouldContain("\"Status\":2");
        }

        [Fact]
        public void ToJson_Newtonsoft_SnakeLower_Indented()
        {
            // Arrange
            var order = new Order
            {
                OrderId = "C-003",
                CreatedUtc = new DateTime(2025, 3, 5, 7, 45, 0, DateTimeKind.Utc),
                Status = OrderStatus.New
            };

            // Act
            string json = order.ToJson(
                JsonExtension.JsonEngine.NewtonsoftJson,
                JsonExtension.JsonCase.SnakeCaseLower,
                indented: true,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: true);

            // Assert
            json.ShouldContain("\"order_id\"");
            json.ShouldContain("\n"); // indented
            json.ShouldContain("\"status\":\"New\"");
        }

        [Fact]
        public void FromJson_Generic_AutoDetect_SnakeLower_Newtonsoft()
        {
            // Arrange: snake_case lower
            var json = "{\"order_id\":\"S-100\",\"created_utc\":\"2025-02-02T08:00:00Z\",\"status\":\"Processing\"}";

            // Act
            var back = json.FromJson<Order>(
                JsonExtension.JsonEngine.NewtonsoftJson,
                JsonExtension.JsonCase.Auto,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: true);

            // Assert
            back.ShouldNotBeNull();
            back!.OrderId.ShouldBe("S-100");
            back.Status.ShouldBe(OrderStatus.Processing);
        }

        [Fact]
        public void FromJson_NonGeneric_KebabLower_Newtonsoft()
        {
            // Arrange: kebab-case lower
            var json = "{\"order-id\":\"K-9\",\"created-utc\":\"2025-12-31T23:59:59Z\",\"status\":\"Done\"}";

            // Act
            var obj = json.FromJson(
                typeof(Order),
                JsonExtension.JsonEngine.NewtonsoftJson,
                JsonExtension.JsonCase.Auto,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: true);

            // Assert
            obj.ShouldBeOfType<Order>();
            var order = (Order)obj!;
            order.OrderId.ShouldBe("K-9");
            order.Status.ShouldBe(OrderStatus.Done);
        }

        [Fact]
        public void DetectJsonCase_ArrayKebabLower_ReturnsKebabLower()
        {
            // Arrange: array root with kebab-case
            var json = "[{\"order-id\":\"A\"},{\"order-id\":\"B\"}]";

            // Act
            var detected = JsonExtension.DetectJsonCase(json);

            // Assert
            detected.ShouldBe(JsonExtension.JsonCase.KebabCaseLower);
        }

        [Fact]
        public void DetectJsonCase_InvalidJson_FallbackCamel()
        {
            // Arrange
            var json = "{ not-a-json }";

            // Act
            var detected = JsonExtension.DetectJsonCase(json);

            // Assert (fallback default)
            detected.ShouldBe(JsonExtension.JsonCase.CamelCase);
        }

        [Fact]
        public void CreateStjOptions_EnumConverterPresence()
        {
            // Arrange & Act
            var withEnum = JsonExtension.CreateStjOptions(JsonExtension.JsonCase.CamelCase, indented: false, dateFormat: null, enumAsString: true);
            var withoutEnum = JsonExtension.CreateStjOptions(JsonExtension.JsonCase.CamelCase, indented: false, dateFormat: null, enumAsString: false);

            // Assert
            withEnum.Converters.ShouldContain(c => c is JsonStringEnumConverter);
            withoutEnum.Converters.ShouldNotContain(c => c is JsonStringEnumConverter);
        }

        [Fact]
        public void CreateStjOptions_DateFormat_AppliedOnSerialize()
        {
            // Arrange
            var opts = JsonExtension.CreateStjOptions(JsonExtension.JsonCase.CamelCase, indented: false, dateFormat: "yyyy/MM/dd HH:mm:ss", enumAsString: true);
            var payload = new { When = new DateTime(2025, 8, 16, 1, 2, 3, DateTimeKind.Utc) };

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(payload, opts);

            // Assert
            json.ShouldContain("\"when\":\"2025/08/16 01:02:03\"");
        }

        [Fact]
        public void CreateNewtonsoftSettings_SnakeCaseUpper_ApplyOnSerialize()
        {
            // Arrange
            var settings = JsonExtension.CreateNewtonsoftSettings(JsonExtension.JsonCase.SnakeCaseUpper, indented: false, dateFormat: "yyyy-MM-dd", enumAsString: true);
            var payload = new { OrderId = "X", CreatedUtc = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc) };

            // Act
            var json = JsonConvert.SerializeObject(payload, settings);

            // Assert: keys upper snake
            json.ShouldContain("\"ORDER_ID\"");
            json.ShouldContain("\"CREATED_UTC\"");
        }

        [Fact]
        public void FromJson_STJ_EnumNumeric_Succeeds()
        {
            // Arrange: enum numeric value
            var json = "{\"orderId\":\"N-1\",\"createdUtc\":\"2025-01-01T00:00:00Z\",\"status\":2}";

            // Act
            var back = json.FromJson<Order>(
                JsonExtension.JsonEngine.SystemTextJson,
                JsonExtension.JsonCase.Auto,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: false);

            // Assert
            back.ShouldNotBeNull();
            back!.Status.ShouldBe(OrderStatus.Done);
        }

        [Fact]
        public void DetectJsonCase_SnakeUpperPreference_WhenUpperDominates()
        {
            // Arrange
            var json = "{\"FOO_BAR\":1,\"BAZ_QUX\":2}";

            // Act
            var detected = JsonExtension.DetectJsonCase(json);

            // Assert
            detected.ShouldBe(JsonExtension.JsonCase.SnakeCaseUpper);
        }

        [Fact(Skip = "Benchmark - skip in CI"), Trait("Category", "Benchmark")]
        public void Benchmark_ToJson_STJ_1000Items()
        {
            // Arrange
            var list = new Order[1000];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = new Order
                {
                    OrderId = $"ORD-{i:0000}",
                    CreatedUtc = new DateTime(2025, 1, 1).AddMinutes(i),
                    Status = (OrderStatus)(i % 3)
                };
            }

            // Act
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var json = list.ToJson(
                JsonExtension.JsonEngine.SystemTextJson,
                JsonExtension.JsonCase.CamelCase,
                indented: false,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: true);
            sw.Stop();

            // Assert
            json.Length.ShouldBeGreaterThan(0);
            sw.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(0);
        }

        // Compatibility: Trailing commas behavior differs
        [Fact, Trait("Category", "Compatibility")]
        public void FromJson_Newtonsoft_TrailingComma_Succeeds()
        {
            // Arrange: trailing comma after last property
            var json = "{\"order_id\":\"X-1\",\"status\":\"New\",}";

            // Act: Newtonsoft ignores trailing commas by default
            var back = json.FromJson<Order>(
                JsonExtension.JsonEngine.NewtonsoftJson,
                JsonExtension.JsonCase.Auto,
                dateFormat: null,
                enumAsString: true);

            // Assert
            back.ShouldNotBeNull();
            back!.Status.ShouldBe(OrderStatus.New);
        }

        [Fact, Trait("Category", "Compatibility")]
        public void FromJson_STJ_TrailingComma_Throws()
        {
            // Arrange
            var json = "{\"orderId\":\"X-1\",\"status\":\"New\",}";

            // Act & Assert: System.Text.Json should throw
            Should.Throw<System.Text.Json.JsonException>(() =>
                json.FromJson<Order>(
                    JsonExtension.JsonEngine.SystemTextJson,
                    JsonExtension.JsonCase.CamelCase,
                    dateFormat: null,
                    enumAsString: true));
        }

        // Enum case-insensitive string values
        [Theory, Trait("Category", "Enum")]
        [InlineData("Processing")]
        [InlineData("processing")]
        [InlineData("PROCESSING")]
        public void FromJson_STJ_EnumString_CaseInsensitive_VariousCases(string value)
        {
            var json = $"{{\"orderId\":\"E-1\",\"createdUtc\":\"2025-01-01T00:00:00Z\",\"status\":\"{value}\"}}";
            var back = json.FromJson<Order>(
                JsonExtension.JsonEngine.SystemTextJson,
                JsonExtension.JsonCase.CamelCase,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: true);
            back.ShouldNotBeNull();
            back!.Status.ShouldBe(OrderStatus.Processing);
        }

        [Theory, Trait("Category", "Enum")]
        [InlineData("Processing")]
        [InlineData("processing")]
        [InlineData("PROCESSING")]
        public void FromJson_Newtonsoft_EnumString_CaseInsensitive_VariousCases(string value)
        {
            var json = $"{{\"order_id\":\"E-2\",\"created_utc\":\"2025-01-01T00:00:00Z\",\"status\":\"{value}\"}}";
            var back = json.FromJson<Order>(
                JsonExtension.JsonEngine.NewtonsoftJson,
                JsonExtension.JsonCase.Auto,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: true);
            back.ShouldNotBeNull();
            back!.Status.ShouldBe(OrderStatus.Processing);
        }

        // Enum numeric while enumAsString = true
        [Theory, Trait("Category", "Enum")]
        [InlineData("2")]
        [InlineData(2)]
        public void FromJson_STJ_EnumNumeric_WithEnumAsString_Allowed(object statusValue)
        {
            var statusJson = statusValue is string s ? $"\"{s}\"" : statusValue.ToString();
            var json = $"{{\"orderId\":\"NN-1\",\"createdUtc\":\"2025-01-01T00:00:00Z\",\"status\":{statusJson}}}";
            var back = json.FromJson<Order>(
                JsonExtension.JsonEngine.SystemTextJson,
                JsonExtension.JsonCase.CamelCase,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: true);
            back.ShouldNotBeNull();
            back!.Status.ShouldBe(OrderStatus.Done);
        }

        [Theory, Trait("Category", "Enum")]
        [InlineData("2")]
        [InlineData(2)]
        public void FromJson_Newtonsoft_EnumNumeric_WithEnumAsString_Allowed(object statusValue)
        {
            var statusJson = statusValue is string s ? $"\"{s}\"" : statusValue.ToString();
            var json = $"{{\"order_id\":\"NN-2\",\"created_utc\":\"2025-01-01T00:00:00Z\",\"status\":{statusJson}}}";
            var back = json.FromJson<Order>(
                JsonExtension.JsonEngine.NewtonsoftJson,
                JsonExtension.JsonCase.Auto,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: true);
            back.ShouldNotBeNull();
            back!.Status.ShouldBe(OrderStatus.Done);
        }

        // Dictionary key naming (Newtonsoft only)
        [Fact, Trait("Category", "Dictionary")]
        public void ToJson_Newtonsoft_DictionaryKey_KebabUpper()
        {
            var dict = new Dictionary<string, int> { ["OrderId"] = 1, ["CreatedUtc"] = 2 };
            string json = dict.ToJson(
                JsonExtension.JsonEngine.NewtonsoftJson,
                JsonExtension.JsonCase.KebabCaseUpper,
                indented: false,
                dateFormat: null,
                enumAsString: true);
            json.ShouldContain("\"ORDER-ID\"");
            json.ShouldContain("\"CREATED-UTC\"");
        }

        [Fact, Trait("Category", "Dictionary")]
        public void ToJson_Newtonsoft_DictionaryKey_SnakeLower()
        {
            var dict = new Dictionary<string, int> { ["OrderId"] = 1 };
            string json = dict.ToJson(
                JsonExtension.JsonEngine.NewtonsoftJson,
                JsonExtension.JsonCase.SnakeCaseLower,
                indented: false,
                dateFormat: null,
                enumAsString: true);
            json.ShouldContain("\"order_id\"");
        }

        // DateTimeOffset custom format (STJ)
        [Fact, Trait("Category", "DateTime")]
        public void CreateStjOptions_DateTimeOffset_CustomFormat_AppliedOnSerialize()
        {
            var opts = JsonExtension.CreateStjOptions(JsonExtension.JsonCase.CamelCase, indented: false, dateFormat: "yyyy-MM-dd HH:mm", enumAsString: true);
            var payload = new { When = new DateTimeOffset(2025, 08, 16, 01, 02, 00, TimeSpan.FromHours(7)) };
            var json = System.Text.Json.JsonSerializer.Serialize(payload, opts);
            json.ShouldContain("\"when\":\"2025-08-16 01:02\"");
        }

        // Date parsing tolerance (STJ): ISO input while custom format configured
        [Fact, Trait("Category", "DateTime")]
        public void FromJson_STJ_DateFormat_Tolerant_ReadsISO()
        {
            var json = "{\"when\":\"2025-08-16T01:02:03Z\"}";
            var type = new { when = default(DateTime) }.GetType();
            var obj = json.FromJson(
                type,
                JsonExtension.JsonEngine.SystemTextJson,
                JsonExtension.JsonCase.CamelCase,
                dateFormat: "yyyy/MM/dd HH:mm:ss",
                enumAsString: true);
            obj.ShouldNotBeNull();
        }

        // Null handling: nulls are emitted
        [Fact, Trait("Category", "NullHandling")]
        public void ToJson_STJ_NullValues_AreEmitted()
        {
            var order = new Order { OrderId = null, CreatedUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), Status = OrderStatus.New };
            var json = order.ToJson(JsonExtension.JsonEngine.SystemTextJson, JsonExtension.JsonCase.CamelCase, indented: false, dateFormat: null, enumAsString: true);
            json.ShouldContain("\"orderId\":null");
        }

        // Missing/Unknown properties behavior
        [Fact, Trait("Category", "Compatibility")]
        public void FromJson_STJ_MissingAndUnknownProperties_Behavior()
        {
            // Missing 'status', unknown 'extra'
            var json = "{\"orderId\":\"M-1\",\"createdUtc\":\"2025-01-01T00:00:00Z\",\"extra\":123}";
            var back = json.FromJson<Order>(JsonExtension.JsonEngine.SystemTextJson, JsonExtension.JsonCase.CamelCase, dateFormat: null, enumAsString: true);
            back.ShouldNotBeNull();
            back!.Status.ShouldBe(OrderStatus.New); // default
        }

        // DetectJsonCase: empty / no keys
        [Fact, Trait("Category", "Detection")]
        public void DetectJsonCase_EmptyObject_FallbackCamel()
        {
            JsonExtension.DetectJsonCase("{}").ShouldBe(JsonExtension.JsonCase.CamelCase);
        }

        [Fact, Trait("Category", "Detection")]
        public void DetectJsonCase_EmptyArray_FallbackCamel()
        {
            JsonExtension.DetectJsonCase("[]").ShouldBe(JsonExtension.JsonCase.CamelCase);
        }

        [Fact, Trait("Category", "Detection")]
        public void DetectJsonCase_PrimitiveArray_FallbackCamel()
        {
            JsonExtension.DetectJsonCase("[1,2,3]").ShouldBe(JsonExtension.JsonCase.CamelCase);
        }

        // Enum string not affected by naming policy (Newtonsoft)
        [Fact, Trait("Category", "Enum")]
        public void ToJson_Newtonsoft_EnumString_NotAffectedByNamingPolicy_KebabUpper()
        {
            var order = new Order { OrderId = "E-UP", CreatedUtc = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc), Status = OrderStatus.Processing };
            var json = order.ToJson(
                JsonExtension.JsonEngine.NewtonsoftJson,
                JsonExtension.JsonCase.KebabCaseUpper,
                indented: false,
                dateFormat: null,
                enumAsString: true);
            json.ShouldContain("\"STATUS\":\"Processing\"");
        }

        // PropertyNameCaseInsensitive
        [Fact, Trait("Category", "CaseInsensitive")]
        public void FromJson_STJ_PropertyNameCaseInsensitive_Works()
        {
            // PascalCase JSON, CamelCase naming
            var json = "{\"OrderId\":\"C-1\",\"CreatedUtc\":\"2025-01-01T00:00:00Z\",\"Status\":\"Done\"}";
            var back = json.FromJson<Order>(JsonExtension.JsonEngine.SystemTextJson, JsonExtension.JsonCase.CamelCase, dateFormat: null, enumAsString: true);
            back.ShouldNotBeNull();
            back!.Status.ShouldBe(OrderStatus.Done);
        }

        // Array root naming applied (Newtonsoft, snake)
        [Fact, Trait("Category", "Array")]
        public void ToJson_Newtonsoft_ArrayRoot_NamingAppliedInside_SnakeLower()
        {
            var list = new List<Order>
            {
                new Order { OrderId = "A", CreatedUtc = new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc), Status = OrderStatus.New },
                new Order { OrderId = "B", CreatedUtc = new DateTime(2025,1,2,0,0,0,DateTimeKind.Utc), Status = OrderStatus.Done }
            };
            var json = list.ToJson(
                JsonExtension.JsonEngine.NewtonsoftJson,
                JsonExtension.JsonCase.SnakeCaseLower,
                indented: false,
                dateFormat: "yyyy-MM-dd'T'HH:mm:ss'Z'",
                enumAsString: true);
            json.ShouldContain("\"order_id\"");
            json.ShouldContain("\"created_utc\"");
        }
    }
}
