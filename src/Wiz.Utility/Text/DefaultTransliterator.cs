using System;

namespace Wiz.Utility.Text
{
    /// <summary>
    /// Default (no-op) transliterator. Use an external library (e.g., Unidecode.NET)
    /// by implementing IStringTransliterator and supplying it to consumers.
    /// </summary>
    public sealed class DefaultTransliterator : IStringTransliterator
    {
        public string Transliterate(string input) => input ?? string.Empty;
    }
}
