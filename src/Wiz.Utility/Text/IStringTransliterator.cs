using System;

namespace Wiz.Utility.Text
{
    /// <summary>
    /// Abstraction for transliteration: convert unicode strings to ASCII or target script.
    /// Provide an implementation (e.g., Unidecode.NET) to improve cross-script slugging.
    /// </summary>
    public interface IStringTransliterator
    {
        string Transliterate(string input);
    }
}
