using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TalkToken;

/// <summary>Shared System.Text.Json options for persona JSON and provider payloads.</summary>
internal static class PersonaJson
{
    /// <summary>
    /// Indented snake_case options for persona files. The Python port writes and reads
    /// exactly the same format, so persona files are interchangeable between both runtimes.
    /// </summary>
    public static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    /// <summary>Compact options used for provider request payloads.</summary>
    public static readonly JsonSerializerOptions Compact = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}

/// <summary>Serialization DTO: flat, versioned, snake_case JSON for <see cref="BuiltPersona"/>.</summary>
internal sealed class PersonaDto
{
    public int SchemaVersion { get; set; } = 1;

    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Role { get; set; }
    public string? Background { get; set; }
    public string? Tone { get; set; }
    public string? Style { get; set; }
    public string? Quirk { get; set; }
    public Verbosity Verbosity { get; set; } = Verbosity.Balanced;
    public string? Language { get; set; }
    public List<string> Rules { get; set; } = [];
    public List<string> Prohibitions { get; set; } = [];
    public List<string> PreferredTerms { get; set; } = [];
    public List<string> BannedTerms { get; set; } = [];
    public List<string> Knowledge { get; set; } = [];
    public List<DialogueExample> Examples { get; set; } = [];
    public string? OutputFormat { get; set; }
    public string? Greeting { get; set; }

    public static PersonaDto From(BuiltPersona persona) => new()
    {
        Name = persona.Name,
        Description = persona.Description,
        Role = persona.Role,
        Background = persona.Background,
        Tone = persona.Tone,
        Style = persona.Style,
        Quirk = persona.Quirk,
        Verbosity = persona.Verbosity,
        Language = persona.Language,
        Rules = [.. persona.Rules],
        Prohibitions = [.. persona.Prohibitions],
        PreferredTerms = [.. persona.PreferredTerms],
        BannedTerms = [.. persona.BannedTerms],
        Knowledge = [.. persona.Knowledge],
        Examples = [.. persona.Examples],
        OutputFormat = persona.OutputFormat,
        Greeting = persona.Greeting,
    };

    public BuiltPersona ToBuiltPersona()
    {
        var name = Name?.Trim() ?? "";
        if (name.Length == 0)
            throw new ArgumentException("Persona JSON must contain a non-empty 'name' field.");

        return new BuiltPersona
        {
            Name = name,
            Description = Clean(Description),
            Role = Clean(Role),
            Background = Clean(Background),
            Tone = Clean(Tone),
            Style = Clean(Style),
            Quirk = Clean(Quirk),
            Verbosity = Verbosity,
            Language = Clean(Language),
            Rules = CleanList(Rules),
            Prohibitions = CleanList(Prohibitions),
            PreferredTerms = CleanList(PreferredTerms),
            BannedTerms = CleanList(BannedTerms),
            Knowledge = CleanList(Knowledge),
            Examples = CleanExamples(Examples),
            OutputFormat = Clean(OutputFormat),
            Greeting = Clean(Greeting),
        };
    }

    public static PersonaDto Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<PersonaDto>(json, PersonaJson.Indented)
                   ?? throw new ArgumentException("Persona JSON is empty or invalid.");
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Persona JSON is invalid: {ex.Message}", ex);
        }
    }

    internal static string? Clean(string? text)
    {
        var trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    internal static List<string> CleanList(IEnumerable<string>? items) =>
        (items ?? []).Select(Clean).Where(item => item is not null).Select(item => item!).ToList();

    private static List<DialogueExample> CleanExamples(IEnumerable<DialogueExample>? examples)
    {
        var result = new List<DialogueExample>();
        foreach (var example in examples ?? [])
        {
            var user = Clean(example?.User);
            var assistant = Clean(example?.Assistant);
            if (user is not null && assistant is not null) result.Add(new DialogueExample(user, assistant));
        }
        return result;
    }
}
