using TalkToken;

// Builds the shared sample persona (Nova) and shows everything the library can do:
// render the system prompt, estimate tokens, export provider payloads, persist to JSON.
var persona = SamplePersonas.Nova();

Console.WriteLine("=== TalkToken demo ==============================");
Console.WriteLine($"Persona: {persona.Name} (estimated ~{persona.EstimateTokens()} tokens)");
Console.WriteLine();
Console.WriteLine("--- System prompt --------------------------------");
Console.WriteLine(persona.RenderSystemPrompt());
Console.WriteLine();
Console.WriteLine("--- OpenAI request -------------------------------");
Console.WriteLine(persona.ToOpenAi("gpt-4o-mini"));
Console.WriteLine();
Console.WriteLine("--- Anthropic request ----------------------------");
Console.WriteLine(persona.ToAnthropic("claude-sonnet-4-5"));
Console.WriteLine();
Console.WriteLine("--- Gemini request -------------------------------");
Console.WriteLine(persona.ToGemini());
Console.WriteLine();
Console.WriteLine("--- Persistence ----------------------------------");
File.WriteAllText("persona.json", persona.ToJson());
Console.WriteLine("Saved persona to persona.json");
var loaded = BuiltPersona.LoadFrom("persona.json");
Console.WriteLine(
    $"Reloaded: name={loaded.Name}, rules={loaded.Rules.Count}, examples={loaded.Examples.Count}, " +
    $"renders identically={loaded.RenderSystemPrompt() == persona.RenderSystemPrompt()}");
