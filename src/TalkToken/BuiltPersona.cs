using System.Text.Json;

namespace TalkToken;

/// <summary>
/// An immutable, ready-to-use persona definition. Create one with <see cref="AiPersona"/>,
/// render it with <see cref="RenderSystemPrompt"/>, export it to a provider request payload,
/// or persist it with <see cref="ToJson"/> / <see cref="FromJson"/>.
/// </summary>
public sealed class BuiltPersona
{
    /// <summary>Current persona JSON schema version.</summary>
    public const int SchemaVersion = 1;

    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Role { get; init; }
    public string? Background { get; init; }
    public string? Tone { get; init; }
    public string? Style { get; init; }
    public string? Quirk { get; init; }
    public Verbosity Verbosity { get; init; } = Verbosity.Balanced;
    public string? Language { get; init; }
    public IReadOnlyList<string> Rules { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Prohibitions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PreferredTerms { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> BannedTerms { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Knowledge { get; init; } = Array.Empty<string>();
    public IReadOnlyList<DialogueExample> Examples { get; init; } = Array.Empty<DialogueExample>();
    public string? OutputFormat { get; init; }
    public string? Greeting { get; init; }

    /// <summary>
    /// Renders the deterministic Markdown-style system prompt. Sections appear in a fixed
    /// order and empty sections are omitted, so the output is byte-identical to the Python port.
    /// </summary>
    public string RenderSystemPrompt()
    {
        var sections = new List<string>();

        var header = "# " + Name;
        if (Description is not null) header += "\n\n" + Description;
        sections.Add(header);

        if (Role is not null) sections.Add("## Role\n" + Role);
        if (Background is not null) sections.Add("## Background\n" + Background);

        var toneLines = new List<string>();
        if (Tone is not null) toneLines.Add("- Tone: " + Tone);
        if (Style is not null) toneLines.Add("- Style: " + Style);
        if (Quirk is not null) toneLines.Add("- Speech quirk: " + Quirk);
        var verbosityInstruction = VerbosityText.Instruction(Verbosity);
        if (verbosityInstruction is not null) toneLines.Add("- Verbosity: " + verbosityInstruction);
        if (toneLines.Count > 0) sections.Add("## Persona & Tone\n" + string.Join("\n", toneLines));

        if (Language is not null) sections.Add("## Language\nAlways respond in " + Language + ".");

        if (Rules.Count > 0)
        {
            var lines = new List<string>();
            for (var i = 0; i < Rules.Count; i++) lines.Add((i + 1) + ". " + Rules[i]);
            sections.Add("## Rules\n" + string.Join("\n", lines));
        }

        if (Prohibitions.Count > 0) sections.Add("## Never Do\n" + Bullets(Prohibitions));

        if (PreferredTerms.Count > 0 || BannedTerms.Count > 0)
        {
            var lines = new List<string>();
            if (PreferredTerms.Count > 0) lines.Add("Use these terms: " + string.Join(", ", PreferredTerms));
            if (BannedTerms.Count > 0) lines.Add("Avoid these terms: " + string.Join(", ", BannedTerms));
            sections.Add("## Vocabulary\n" + string.Join("\n", lines));
        }

        if (Knowledge.Count > 0) sections.Add("## Knowledge\n" + Bullets(Knowledge));

        if (OutputFormat is not null) sections.Add("## Output Format\n" + OutputFormat);
        if (Greeting is not null) sections.Add("## Greeting\nOpen each conversation with this line: " + Greeting);

        if (Examples.Count > 0)
        {
            var blocks = new List<string>();
            for (var i = 0; i < Examples.Count; i++)
            {
                var e = Examples[i];
                blocks.Add($"### Example {i + 1}\n\nUser: {e.User}\n\nAssistant: {e.Assistant}");
            }
            sections.Add("## Examples\n" + string.Join("\n\n", blocks));
        }

        return string.Join("\n\n", sections);
    }

    /// <summary>Estimated token count of the rendered system prompt (heuristic, ~4 chars per token).</summary>
    public int EstimateTokens(double charsPerToken = 4.0)
        => TokenEstimator.Estimate(RenderSystemPrompt(), charsPerToken);

    /// <summary>True when the rendered system prompt fits the given token budget.</summary>
    public bool FitsBudget(int maxTokens, double charsPerToken = 4.0)
        => TokenEstimator.FitsBudget(RenderSystemPrompt(), maxTokens, charsPerToken);

    /// <summary>The system prompt as a single OpenAI-style chat message.</summary>
    public IReadOnlyList<ChatMessage> ToOpenAiMessages()
        => [new ChatMessage("system", RenderSystemPrompt())];

    /// <summary>OpenAI chat.completions request skeleton. Pass a model name to include it.</summary>
    public string ToOpenAi(string? model = null)
    {
        var payload = new Dictionary<string, object?>();
        if (model is not null) payload["model"] = model;
        payload["messages"] = new List<object>
        {
            new Dictionary<string, string> { ["role"] = "system", ["content"] = RenderSystemPrompt() },
        };
        return JsonSerializer.Serialize(payload, PersonaJson.Compact);
    }

    /// <summary>
    /// Anthropic messages request skeleton: the persona goes into "system", the conversation
    /// turns belong in "messages". Pass a model name to include it.
    /// </summary>
    public string ToAnthropic(string? model = null, int maxTokens = 1024)
    {
        var payload = new Dictionary<string, object?>();
        if (model is not null) payload["model"] = model;
        payload["max_tokens"] = maxTokens;
        payload["system"] = RenderSystemPrompt();
        payload["messages"] = new List<object>();
        return JsonSerializer.Serialize(payload, PersonaJson.Compact);
    }

    /// <summary>
    /// Gemini generateContent request skeleton. The model name goes in the REST URL
    /// (models/{model}:generateContent), not in the body.
    /// </summary>
    public string ToGemini()
    {
        var payload = new Dictionary<string, object?>
        {
            ["system_instruction"] = new Dictionary<string, object>
            {
                ["parts"] = new List<object>
                {
                    new Dictionary<string, string> { ["text"] = RenderSystemPrompt() },
                },
            },
            ["contents"] = new List<object>(),
        };
        return JsonSerializer.Serialize(payload, PersonaJson.Compact);
    }

    /// <summary>Serializes the persona to versioned snake_case JSON (byte-identical to the Python port).</summary>
    public string ToJson() => JsonSerializer.Serialize(PersonaDto.From(this), PersonaJson.Indented);

    /// <summary>Deserializes a persona from JSON produced by <see cref="ToJson"/> or by the Python port.</summary>
    public static BuiltPersona FromJson(string json) => PersonaDto.Parse(json).ToBuiltPersona();

    /// <summary>Saves the persona JSON to a file.</summary>
    public void SaveTo(string path) => File.WriteAllText(path, ToJson());

    /// <summary>Loads a persona from a JSON file.</summary>
    public static BuiltPersona LoadFrom(string path) => FromJson(File.ReadAllText(path));

    private static string Bullets(IReadOnlyList<string> items)
    {
        var lines = new List<string>();
        foreach (var item in items) lines.Add("- " + item);
        return string.Join("\n", lines);
    }
}
