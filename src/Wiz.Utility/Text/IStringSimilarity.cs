using System;

namespace Wiz.Utility.Text
{
    /// <summary>
    /// Abstraction for string similarity/distance algorithms.
    /// </summary>
    public interface IStringSimilarity
    {
        int LevenshteinDistance(string a, string b);
        double Similarity(string a, string b); // 0.0..1.0
    }
}
