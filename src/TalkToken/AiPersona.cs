namespace TalkToken;

/// <summary>
/// Fluent builder for <see cref="BuiltPersona"/>. Start with <see cref="Create"/>, chain the
/// <c>With*</c> / <c>Add*</c> methods, then call <see cref="Build"/>.
/// </summary>
public sealed class AiPersona
{
    private string _name;
    private string? _description;
    private string? _role;
    private string? _background;
    private string? _tone;
    private string? _style;
    private string? _quirk;
    private Verbosity _verbosity = Verbosity.Balanced;
    private string? _language;
    private readonly List<string> _rules = [];
    private readonly List<string> _prohibitions = [];
    private readonly List<string> _preferredTerms = [];
    private readonly List<string> _bannedTerms = [];
    private readonly List<string> _knowledge = [];
    private readonly List<DialogueExample> _examples = [];
    private string? _outputFormat;
    private string? _greeting;

    private AiPersona(string name) => _name = name;

    /// <summary>Starts building a persona. The name is required and becomes the prompt's top-level heading.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, empty or whitespace.</exception>
    public static AiPersona Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Persona name must not be empty.", nameof(name));
        return new AiPersona(name);
    }

    /// <summary>Short summary of who the persona is, shown right under the name heading.</summary>
    public AiPersona WithDescription(string text) { _description = Clean(text); return this; }

    /// <summary>The job or role the AI plays (rendered under "## Role").</summary>
    public AiPersona WithRole(string text) { _role = Clean(text); return this; }

    /// <summary>Backstory the AI can draw on (rendered under "## Background").</summary>
    public AiPersona WithBackground(string text) { _background = Clean(text); return this; }

    /// <summary>How the AI should sound, e.g. a <see cref="TonePresets"/> value (rendered under "## Persona &amp; Tone").</summary>
    public AiPersona WithTone(string text) { _tone = Clean(text); return this; }

    /// <summary>Writing style details such as sentence length and word choice.</summary>
    public AiPersona WithStyle(string text) { _style = Clean(text); return this; }

    /// <summary>A signature speech habit, e.g. for game NPCs.</summary>
    public AiPersona WithQuirk(string text) { _quirk = Clean(text); return this; }

    /// <summary>Answer length guidance. <see cref="Verbosity.Balanced"/> emits nothing.</summary>
    public AiPersona WithVerbosity(Verbosity verbosity) { _verbosity = verbosity; return this; }

    /// <summary>Language the AI must answer in (rendered under "## Language").</summary>
    public AiPersona WithLanguage(string language) { _language = Clean(language); return this; }

    /// <summary>Behavioral rules, rendered as a numbered list under "## Rules". Blank entries are ignored.</summary>
    public AiPersona WithRules(params string[] rules) { _rules.AddRange(CleanList(rules)); return this; }

    /// <summary>Appends a single rule.</summary>
    public AiPersona AddRule(string rule) { Add(_rules, rule); return this; }

    /// <summary>Things the AI must never do, rendered under "## Never Do".</summary>
    public AiPersona WithProhibitions(params string[] prohibitions) { _prohibitions.AddRange(CleanList(prohibitions)); return this; }

    /// <summary>Appends a single prohibition.</summary>
    public AiPersona AddProhibition(string prohibition) { Add(_prohibitions, prohibition); return this; }

    /// <summary>Vocabulary the AI should prefer (rendered under "## Vocabulary").</summary>
    public AiPersona WithPreferredTerms(params string[] terms) { _preferredTerms.AddRange(CleanList(terms)); return this; }

    /// <summary>Vocabulary the AI should avoid (rendered under "## Vocabulary").</summary>
    public AiPersona WithBannedTerms(params string[] terms) { _bannedTerms.AddRange(CleanList(terms)); return this; }

    /// <summary>Facts the AI may rely on, rendered as a bullet list under "## Knowledge".</summary>
    public AiPersona WithKnowledge(params string[] facts) { _knowledge.AddRange(CleanList(facts)); return this; }

    /// <summary>Appends a single knowledge fact.</summary>
    public AiPersona AddKnowledge(string fact) { Add(_knowledge, fact); return this; }

    /// <summary>Adds one few-shot dialogue example (user turn + ideal assistant turn).</summary>
    public AiPersona WithExample(string user, string assistant)
    {
        var example = CleanExample(user, assistant);
        if (example is not null) _examples.Add(example);
        return this;
    }

    /// <summary>Adds several few-shot dialogue examples.</summary>
    public AiPersona WithExamples(params DialogueExample[] examples)
    {
        foreach (var example in examples)
        {
            var cleaned = CleanExample(example?.User, example?.Assistant);
            if (cleaned is not null) _examples.Add(cleaned);
        }
        return this;
    }

    /// <summary>Expected shape of the AI's answers (rendered under "## Output Format").</summary>
    public AiPersona WithOutputFormat(string text) { _outputFormat = Clean(text); return this; }

    /// <summary>Opening line the AI uses to start a conversation (rendered under "## Greeting").</summary>
    public AiPersona WithGreeting(string text) { _greeting = Clean(text); return this; }

    /// <summary>
    /// Validates and freezes the persona. All strings are trimmed; blank strings and blank
    /// list entries are dropped. Throws when the name is empty.
    /// </summary>
    public BuiltPersona Build()
    {
        var name = _name.Trim();
        if (name.Length == 0)
            throw new InvalidOperationException("Persona name must not be empty.");

        return new BuiltPersona
        {
            Name = name,
            Description = _description,
            Role = _role,
            Background = _background,
            Tone = _tone,
            Style = _style,
            Quirk = _quirk,
            Verbosity = _verbosity,
            Language = _language,
            Rules = [.. _rules],
            Prohibitions = [.. _prohibitions],
            PreferredTerms = [.. _preferredTerms],
            BannedTerms = [.. _bannedTerms],
            Knowledge = [.. _knowledge],
            Examples = [.. _examples],
            OutputFormat = _outputFormat,
            Greeting = _greeting,
        };
    }

    private static void Add(List<string> target, string item)
    {
        var cleaned = Clean(item);
        if (cleaned is not null) target.Add(cleaned);
    }

    private static string? Clean(string? text)
    {
        var trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static IEnumerable<string> CleanList(IEnumerable<string> items) =>
        items.Select(Clean).Where(item => item is not null).Select(item => item!);

    private static DialogueExample? CleanExample(string? user, string? assistant)
    {
        var u = Clean(user);
        var a = Clean(assistant);
        return u is null || a is null ? null : new DialogueExample(u, a);
    }
}
