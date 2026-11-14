namespace TalkToken;

/// <summary>
/// Heuristic token estimator. Real tokenizers (BPE) vary per model; this uses the widely
/// used approximation of about 4 characters per token for English text. Good enough for
/// budgeting context windows - not for exact billing.
/// </summary>
public static class TokenEstimator
{
    /// <summary>Estimates the token count of <paramref name="text"/>. Null or empty text is 0 tokens.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="charsPerToken"/> is not positive.</exception>
    public static int Estimate(string? text, double charsPerToken = 4.0)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        if (charsPerToken <= 0)
            throw new ArgumentOutOfRangeException(nameof(charsPerToken), "charsPerToken must be positive.");
        return (int)Math.Ceiling(text.Length / charsPerToken);
    }

    /// <summary>Returns true when <paramref name="text"/> fits inside a budget of <paramref name="maxTokens"/> tokens.</summary>
    public static bool FitsBudget(string? text, int maxTokens, double charsPerToken = 4.0)
        => Estimate(text, charsPerToken) <= maxTokens;
}
