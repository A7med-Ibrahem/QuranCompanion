using System.Text;

namespace QuranCompanion.Infrastructure.Search;

/// <summary>
/// Search-time Arabic normalization (spec section 9):
///   - أ / إ / آ → ا
///   - ى → ي
///   - diacritics (tashkeel) removed
///
/// This never touches the original Quran text stored in Ayah.Text - it only
/// produces a separate string used for matching, and a position map so a
/// match found in the normalized string can be highlighted at the correct
/// spot in the original (still fully-vocalized) text.
/// </summary>
public static class ArabicTextNormalizer
{
    public static string Normalize(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (IsDiacritic(ch)) continue;
            sb.Append(NormalizeChar(ch));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Same normalization as <see cref="Normalize"/>, but also returns, for every
    /// character in the *normalized* output, the index of the character in the
    /// *original* input that produced it - so a match position found by searching
    /// the normalized text can be mapped back to highlight the original text.
    /// </summary>
    public static (string Normalized, int[] OriginalIndexMap) NormalizeWithMap(string input)
    {
        var sb = new StringBuilder(input.Length);
        var map = new List<int>(input.Length);

        for (var i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            if (IsDiacritic(ch)) continue;

            sb.Append(NormalizeChar(ch));
            map.Add(i);
        }

        return (sb.ToString(), map.ToArray());
    }

    private static char NormalizeChar(char ch) => ch switch
    {
        'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
        'ى' => 'ي',
        _ => ch
    };

    private static bool IsDiacritic(char ch) =>
        ch is (>= '\u064B' and <= '\u065F')  // Arabic combining diacritics (tashkeel)
            or '\u0670'                       // superscript alef
            or (>= '\u06D6' and <= '\u06ED');  // Quranic annotation marks (small high signs, etc.)
}
