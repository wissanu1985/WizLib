using System;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace Wiz.Utility.Extensions;

public static partial class JsonExtension
{
    /// <summary>
    /// Generic delimited-case naming strategy for Newtonsoft.Json.
    /// Supports kebab-case and SNAKE_CASE with configurable casing.
    /// </summary>
    internal sealed class DelimitedCaseNamingStrategy : NamingStrategy
    {
        private readonly char _delimiter;
        private readonly bool _toUpper;

        public DelimitedCaseNamingStrategy(char delimiter, bool upperCase)
        {
            _delimiter = delimiter;
            _toUpper = upperCase;
            ProcessDictionaryKeys = true;
            OverrideSpecifiedNames = false;
            ProcessExtensionDataNames = true;
        }

        protected override string ResolvePropertyName(string name)
            => ConvertName(name);

        // For extension data and dictionary keys, the base NamingStrategy will
        // use ProcessExtensionDataNames/ProcessDictionaryKeys flags together with
        // its internal resolution which is aligned with ResolvePropertyName.

        private string ConvertName(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            // Split PascalCase/camelCase and non-alphanumerics
            var input = s!;
            var result = new StringBuilder(input.Length * 2);
            char? prev = null;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                bool isAlnum = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');

                if (!isAlnum)
                {
                    // delimiter for any non-alnum
                    if (result.Length > 0 && result[^1] != _delimiter)
                        result.Append(_delimiter);
                    prev = _delimiter;
                    continue;
                }

                bool isUpper = c >= 'A' && c <= 'Z';
                bool isLowerOrDigit = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');

                // Insert delimiter between lower/digit followed by upper (camel boundary)
                if (isUpper && prev.HasValue && ((prev >= 'a' && prev <= 'z') || (prev >= '0' && prev <= '9')))
                {
                    if (result.Length > 0 && result[^1] != _delimiter)
                        result.Append(_delimiter);
                }

                // Append transformed char
                char outCh;
                if (_toUpper)
                {
                    outCh = (char)(isLowerOrDigit && c <= 'z' && c >= 'a' ? (c - 32) : c); // to upper if letter
                }
                else
                {
                    outCh = (char)(isUpper ? (c + 32) : c); // to lower if letter
                }

                result.Append(outCh);
                prev = c;
            }

            // collapse duplicate delimiters and trim
            var text = result.ToString();
            // simple collapse
            var collapse = new StringBuilder(text.Length);
            char last = '\0';
            foreach (var ch in text)
            {
                if (ch == _delimiter && last == _delimiter) continue;
                collapse.Append(ch);
                last = ch;
            }
            var trimmed = collapse.ToString().Trim(_delimiter);
            return trimmed;
        }
    }
}
