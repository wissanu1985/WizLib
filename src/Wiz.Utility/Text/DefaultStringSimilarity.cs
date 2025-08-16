using System;

namespace Wiz.Utility.Text
{
    /// <summary>
    /// Default string similarity implementation: Levenshtein distance (iterative, O(n*m))
    /// and a normalized similarity ratio in [0,1]. No allocations beyond two rows.
    /// </summary>
    public sealed class DefaultStringSimilarity : IStringSimilarity
    {
        public int LevenshteinDistance(string a, string b)
        {
            a ??= string.Empty;
            b ??= string.Empty;

            int n = a.Length;
            int m = b.Length;
            if (n == 0) return m;
            if (m == 0) return n;

            // Two-row DP to minimize allocations
            var prev = new int[m + 1];
            var curr = new int[m + 1];

            for (int j = 0; j <= m; j++) prev[j] = j;

            for (int i = 1; i <= n; i++)
            {
                curr[0] = i;
                int ai = a[i - 1];
                for (int j = 1; j <= m; j++)
                {
                    int cost = (ai == b[j - 1]) ? 0 : 1;
                    int insertion = curr[j - 1] + 1;
                    int deletion = prev[j] + 1;
                    int substitution = prev[j - 1] + cost;
                    int val = insertion < deletion ? insertion : deletion;
                    if (substitution < val) val = substitution;
                    curr[j] = val;
                }
                // swap
                var tmp = prev;
                prev = curr;
                curr = tmp;
            }

            return prev[m];
        }

        public double Similarity(string a, string b)
        {
            a ??= string.Empty;
            b ??= string.Empty;
            if (a.Length == 0 && b.Length == 0) return 1.0;
            int dist = LevenshteinDistance(a, b);
            int max = Math.Max(a.Length, b.Length);
            return max == 0 ? 1.0 : 1.0 - ((double)dist / max);
        }
    }
}
