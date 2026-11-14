namespace TalkToken;

/// <summary>How long and detailed the persona's answers should be.</summary>
public enum Verbosity
{
    /// <summary>Default. No verbosity instruction is emitted into the prompt.</summary>
    Balanced,

    /// <summary>Answer in as few words as possible.</summary>
    Terse,

    /// <summary>Keep answers short and to the point.</summary>
    Concise,

    /// <summary>Give thorough, detailed explanations with examples.</summary>
    Elaborate,
}

/// <summary>Maps <see cref="Verbosity"/> values to the instruction line rendered into the prompt.</summary>
internal static class VerbosityText
{
    public static string? Instruction(Verbosity verbosity) => verbosity switch
    {
        Verbosity.Terse => "Answer in as few words as possible.",
        Verbosity.Concise => "Keep answers short and to the point.",
        Verbosity.Elaborate => "Give thorough, detailed explanations with examples.",
        _ => null,
    };
}
