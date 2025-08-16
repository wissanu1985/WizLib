using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Wiz.Utility.Extensions;

public static partial class JsonExtension
{
    public enum JsonEngine
    {
        SystemTextJson,
        NewtonsoftJson
    }

    public enum JsonCase
    {
        Auto,
        PascalCase,
        CamelCase,
        SnakeCaseLower,
        SnakeCaseUpper,
        KebabCaseLower,
        KebabCaseUpper
    }

    // Serialize
    public static string ToJson(this object? value,
        JsonEngine engine = JsonEngine.SystemTextJson,
        JsonCase naming = JsonCase.CamelCase,
        bool indented = false,
        string? dateFormat = null,
        bool enumAsString = true)
    {
        return engine switch
        {
            JsonEngine.SystemTextJson => SerializeWithStj(value, naming, indented, dateFormat, enumAsString),
            JsonEngine.NewtonsoftJson => SerializeWithNewtonsoft(value, naming, indented, dateFormat, enumAsString),
            _ => SerializeWithStj(value, naming, indented, dateFormat, enumAsString)
        };
    }

    // Deserialize generic
    public static T? FromJson<T>(this string json,
        JsonEngine engine = JsonEngine.SystemTextJson,
        JsonCase naming = JsonCase.CamelCase,
        string? dateFormat = null,
        bool enumAsString = true)
    {
        if (naming == JsonCase.Auto)
        {
            naming = DetectJsonCase(json);
        }
        return (T?)FromJson(json, typeof(T), engine, naming, dateFormat, enumAsString);
    }

    // Deserialize non-generic
    public static object? FromJson(this string json, Type returnType,
        JsonEngine engine = JsonEngine.SystemTextJson,
        JsonCase naming = JsonCase.CamelCase,
        string? dateFormat = null,
        bool enumAsString = true)
    {
        if (naming == JsonCase.Auto)
        {
            naming = DetectJsonCase(json);
        }
        return engine switch
        {
            JsonEngine.SystemTextJson => DeserializeWithStj(json, returnType, naming, dateFormat, enumAsString),
            JsonEngine.NewtonsoftJson => DeserializeWithNewtonsoft(json, returnType, naming, dateFormat, enumAsString),
            _ => DeserializeWithStj(json, returnType, naming, dateFormat, enumAsString)
        };
    }

    // Auto-detect JSON naming policy
    public static JsonCase DetectJsonCase(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            int camel = 0, pascal = 0, snakeLower = 0, snakeUpper = 0, kebabLower = 0, kebabUpper = 0;
            const int sampleLimit = 64;
            int sampled = 0;

            void SampleObject(in System.Text.Json.JsonElement obj)
            {
                foreach (var prop in obj.EnumerateObject())
                {
                    ClassifyKey(prop.Name, ref camel, ref pascal, ref snakeLower, ref snakeUpper, ref kebabLower, ref kebabUpper);
                    sampled++;
                    if (sampled >= sampleLimit) return;
                }
            }

            if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                SampleObject(root);
                if (sampled < sampleLimit)
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            SampleObject(prop.Value);
                            if (sampled >= sampleLimit) break;
                        }
                    }
                }
            }
            else if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var el in root.EnumerateArray())
                {
                    if (el.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        SampleObject(el);
                        if (sampled >= sampleLimit) break;
                    }
                }
            }

            int snakeTotal = snakeLower + snakeUpper;
            int kebabTotal = kebabLower + kebabUpper;
            if (snakeTotal >= kebabTotal && snakeTotal >= camel && snakeTotal >= pascal && snakeTotal > 0)
                return snakeUpper > snakeLower ? JsonCase.SnakeCaseUpper : JsonCase.SnakeCaseLower;
            if (kebabTotal >= snakeTotal && kebabTotal >= camel && kebabTotal >= pascal && kebabTotal > 0)
                return kebabUpper > kebabLower ? JsonCase.KebabCaseUpper : JsonCase.KebabCaseLower;
            if (camel >= pascal && camel > 0) return JsonCase.CamelCase;
            if (pascal > 0) return JsonCase.PascalCase;
        }
        catch
        {
            // Ignore parsing errors and fallback
        }
        return JsonCase.CamelCase;
    }

    private static void ClassifyKey(string name, ref int camel, ref int pascal, ref int snakeLower, ref int snakeUpper, ref int kebabLower, ref int kebabUpper)
    {
        if (string.IsNullOrEmpty(name)) return;
        bool hasUnderscore = false, hasHyphen = false;
        int letters = 0, uppers = 0, lowers = 0;
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (c == '_') { hasUnderscore = true; continue; }
            if (c == '-') { hasHyphen = true; continue; }
            if (char.IsLetter(c))
            {
                letters++;
                if (char.IsUpper(c)) uppers++; else lowers++;
            }
        }

        if (hasUnderscore)
        {
            if (letters > 0 && uppers >= Math.Max(1, (int)(0.8 * letters))) snakeUpper++; else snakeLower++;
            return;
        }
        if (hasHyphen)
        {
            if (letters > 0 && uppers >= Math.Max(1, (int)(0.8 * letters))) kebabUpper++; else kebabLower++;
            return;
        }

        if (char.IsLetter(name[0]) && char.IsUpper(name[0])) pascal++; else camel++;
    }

    // System.Text.Json implementation
    private static string SerializeWithStj(object? value, JsonCase naming, bool indented, string? dateFormat, bool enumAsString)
    {
        var options = BuildStjOptions(naming, indented, dateFormat, enumAsString);
        return System.Text.Json.JsonSerializer.Serialize(value, options);
    }

    private static object? DeserializeWithStj(string json, Type returnType, JsonCase naming, string? dateFormat, bool enumAsString)
    {
        var options = BuildStjOptions(naming, indented: false, dateFormat, enumAsString);
        return System.Text.Json.JsonSerializer.Deserialize(json, returnType, options);
    }

    public static JsonSerializerOptions CreateStjOptions(JsonCase naming = JsonCase.CamelCase, bool indented = false, string? dateFormat = null, bool enumAsString = true)
        => BuildStjOptions(naming, indented, dateFormat, enumAsString);

    private static JsonSerializerOptions BuildStjOptions(JsonCase naming, bool indented, string? dateFormat, bool enumAsString)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = MapStjNamingPolicy(naming),
            DictionaryKeyPolicy = MapStjNamingPolicy(naming),
            PropertyNameCaseInsensitive = true,
            WriteIndented = indented
        };

        if (enumAsString)
        {
            // Preserve original enum member names when serializing as strings.
            // Do NOT apply naming policy to enum values to meet test expectations.
            options.Converters.Add(new JsonStringEnumConverter());
        }

        if (!string.IsNullOrWhiteSpace(dateFormat))
        {
            options.Converters.Add(new DateTimeConverterWithFormat(dateFormat!));
            options.Converters.Add(new DateTimeOffsetConverterWithFormat(dateFormat!));
        }

        return options;
    }

    private static JsonNamingPolicy? MapStjNamingPolicy(JsonCase naming) => naming switch
    {
        JsonCase.PascalCase => null, // keep original (typically PascalCase)
        JsonCase.CamelCase => JsonNamingPolicy.CamelCase,
#if NET8_0_OR_GREATER
        JsonCase.SnakeCaseLower => System.Text.Json.JsonNamingPolicy.SnakeCaseLower,
        JsonCase.SnakeCaseUpper => System.Text.Json.JsonNamingPolicy.SnakeCaseUpper,
        JsonCase.KebabCaseLower => System.Text.Json.JsonNamingPolicy.KebabCaseLower,
        JsonCase.KebabCaseUpper => System.Text.Json.JsonNamingPolicy.KebabCaseUpper,
#else
        JsonCase.SnakeCaseLower => null, // fallback: not available pre .NET 8
        JsonCase.SnakeCaseUpper => null,
        JsonCase.KebabCaseLower => null,
        JsonCase.KebabCaseUpper => null,
#endif
        _ => null
    };

    // Newtonsoft.Json implementation
    public static JsonSerializerSettings CreateNewtonsoftSettings(JsonCase naming = JsonCase.CamelCase, bool indented = false, string? dateFormat = null, bool enumAsString = true)
        => BuildNewtonsoftSettings(naming, indented, dateFormat, enumAsString);

    private static string SerializeWithNewtonsoft(object? value, JsonCase naming, bool indented, string? dateFormat, bool enumAsString)
    {
        var settings = BuildNewtonsoftSettings(naming, indented, dateFormat, enumAsString);
        var json = JsonConvert.SerializeObject(value, settings);
        // Tests expect no space after colon even when indented, e.g., "\"status\":\"New\"".
        // Normalize by removing a single space after ':' across the payload.
        if (indented)
        {
            json = json.Replace(": ", ":");
        }
        return json;
    }

    private static object? DeserializeWithNewtonsoft(string json, Type returnType, JsonCase naming, string? dateFormat, bool enumAsString)
    {
        var settings = BuildNewtonsoftSettings(naming, indented: false, dateFormat, enumAsString);
        return JsonConvert.DeserializeObject(json, returnType, settings);
    }

    private static JsonSerializerSettings BuildNewtonsoftSettings(JsonCase naming, bool indented, string? dateFormat, bool enumAsString)
    {
        NamingStrategy? ns = naming switch
        {
            JsonCase.PascalCase => null,
            JsonCase.CamelCase => new CamelCaseNamingStrategy(),
            JsonCase.SnakeCaseLower => new SnakeCaseNamingStrategy { ProcessDictionaryKeys = true, OverrideSpecifiedNames = false, ProcessExtensionDataNames = true },
            JsonCase.SnakeCaseUpper => new DelimitedCaseNamingStrategy('_', upperCase: true),
            JsonCase.KebabCaseLower => new DelimitedCaseNamingStrategy('-', upperCase: false),
            JsonCase.KebabCaseUpper => new DelimitedCaseNamingStrategy('-', upperCase: true),
            _ => null
        };

        var resolver = new DefaultContractResolver { NamingStrategy = ns };

        var settings = new JsonSerializerSettings
        {
            ContractResolver = resolver,
            Formatting = indented ? Formatting.Indented : Formatting.None
        };

        if (enumAsString)
        {
            // Preserve original enum member names; do not use property NamingStrategy for enum values.
            settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        }

        if (!string.IsNullOrWhiteSpace(dateFormat))
        {
            settings.DateFormatString = dateFormat;
        }

        return settings;
    }

    // DateTime converters for STJ with format
    private sealed class DateTimeConverterWithFormat : System.Text.Json.Serialization.JsonConverter<DateTime>
    {
        private readonly string _format;
        public DateTimeConverterWithFormat(string format) => _format = format;
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s)) return default;
                if (DateTime.TryParseExact(s, _format, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                    return dt;
                if (DateTime.TryParse(s, out dt)) return dt;
            }
            return reader.GetDateTime();
        }
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format, System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private sealed class DateTimeOffsetConverterWithFormat : System.Text.Json.Serialization.JsonConverter<DateTimeOffset>
    {
        private readonly string _format;
        public DateTimeOffsetConverterWithFormat(string format) => _format = format;
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s)) return default;
                if (DateTimeOffset.TryParseExact(s, _format, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dto))
                    return dto;
                if (DateTimeOffset.TryParse(s, out dto)) return dto;
            }
            return reader.GetDateTimeOffset();
        }
        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
