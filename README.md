## this project was made by GLM 5.3 FLASH, you may use it, study it or fork it as long as it retains the MIT license

# TalkToken

**A fluent library that describes how your AI should talk - in C# and Python.**

TalkToken lets you define an AI persona (role, tone, rules, prohibitions, vocabulary,
knowledge, few-shot examples...) with a chainable builder, then renders it into a clean,
deterministic Markdown-style **system prompt**, exports **ready-to-post request payloads**
for OpenAI, Anthropic and Gemini, persists the persona as **versioned snake_case JSON**,
and estimates the **token count** of the prompt for context-window budgeting.

> **A note on naming.** In AI models, *tokens* are the units of text a model reads and
> writes (roughly 4 characters each), while a description of *how the AI should talk* is a
> **system prompt / persona**. TalkToken covers both halves: it generates the behavioral
> description (the persona prompt) and estimates its token footprint, so you can keep it
> inside your model's context budget.

Both implementations are **verified byte-identical**: the same persona renders to exactly
the same prompt string and saves to exactly the same JSON bytes in C# and Python (enforced
by the shared golden file `csharp/tests/golden_prompt.txt` = `python/tests/golden_prompt.txt`).

## Features

- Fluent builder API (`AiPersona` / `AiPersona.create`)
- Deterministic system-prompt rendering - fixed section order, empty sections omitted
- Provider export adapters: OpenAI `chat.completions`, Anthropic `messages`, Gemini `generateContent`
- JSON persistence with `schema_version`, snake_case keys, cross-language interchangeable
- Heuristic token estimator (~4 chars/token) with budget checks
- Tone presets, verbosity levels, few-shot examples, greeting, output format
- Zero runtime dependencies (C#: only BCL; Python: only stdlib)

## Project layout

```
talktoken/
├── csharp/
│   ├── TalkToken.sln
│   ├── src/TalkToken/            # the library (net8.0, NuGet-packable)
│   ├── tests/TalkToken.Tests/    # 28 xUnit tests + golden_prompt.txt
│   └── samples/TalkToken.Demo/   # runnable console demo
├── python/
│   ├── pyproject.toml
│   ├── src/talktoken/            # the library (pure stdlib, py>=3.10)
│   ├── tests/                    # 28 pytest tests + golden_prompt.txt
│   └── demo.py                   # runnable demo (no install needed)
└── examples/nova.persona.json    # JSON produced by both ports, byte-identical
```

## Quick start - C#

```csharp
using TalkToken;

var persona = AiPersona
    .Create("Nova")
    .WithDescription("A cheerful guide who explains the night sky to beginners.")
    .WithTone(TonePresets.Friendly)
    .WithVerbosity(Verbosity.Concise)
    .WithRules(
        "Explain one concept at a time before moving on.",
        "If you are not sure about a number, say so honestly.")
    .WithProhibitions("Never invent fake astronomy facts.")
    .WithPreferredTerms("telescope", "constellation", "nebula")
    .WithExample("What's that bright star near the moon?",
                 "That is most likely Venus, the brightest planet in our sky.")
    .Build();

string prompt = persona.RenderSystemPrompt();   // the system prompt
int tokens    = persona.EstimateTokens();       // ~4 chars/token heuristic

string openAiRequest   = persona.ToOpenAi("gpt-4o-mini");
string anthropicRequest = persona.ToAnthropic("claude-sonnet-4-5", maxTokens: 1024);
string geminiRequest   = persona.ToGemini();

persona.SaveTo("nova.persona.json");            // and BuiltPersona.LoadFrom(...) to read it back
```

Build and run everything:

```bash
dotnet build csharp/TalkToken.sln
dotnet test  csharp/TalkToken.sln
dotnet run --project csharp/samples/TalkToken.Demo
```

To produce a NuGet package: `dotnet pack csharp/src/TalkToken/TalkToken.csproj`.

## Quick start - Python

```python
from talktoken import AiPersona, TonePresets, Verbosity

persona = (
    AiPersona.create("Nova")
    .with_description("A cheerful guide who explains the night sky to beginners.")
    .with_tone(TonePresets.FRIENDLY)
    .with_verbosity(Verbosity.CONCISE)
    .with_rules(
        "Explain one concept at a time before moving on.",
        "If you are not sure about a number, say so honestly.",
    )
    .with_prohibitions("Never invent fake astronomy facts.")
    .with_preferred_terms("telescope", "constellation", "nebula")
    .with_example("What's that bright star near the moon?",
                  "That is most likely Venus, the brightest planet in our sky.")
    .build()
)

prompt = persona.render_system_prompt()
tokens = persona.estimate_tokens()

openai_payload    = persona.to_openai("gpt-4o-mini")       # dict, ready for requests.post(json=...)
anthropic_payload = persona.to_anthropic("claude-sonnet-4-5", max_tokens=1024)
gemini_payload    = persona.to_gemini()

persona.save_to("nova.persona.json")                       # and BuiltPersona.load_from(...) to read it back
```

Run everything (no install needed; or `pip install -e python` first):

```bash
PYTHONPATH=python/src python3 -m pytest python/tests -q
python3 python/demo.py
```

## What the rendered prompt looks like

The sample persona (`SamplePersonas.Nova()` / `talktoken.samples.nova()`) renders to
exactly this text in both languages (~419 tokens estimated):

```markdown
# Nova

A cheerful guide who explains the night sky to beginners.

## Role
You are Nova, an amateur astronomer hosting a stargazing livestream chat.

## Persona & Tone
- Tone: friendly, warm and encouraging
- Style: short sentences, everyday words, vivid comparisons
- Speech quirk: ends explanations with a fun fact introduced by 'Fun fact:'
- Verbosity: Keep answers short and to the point.

## Language
Always respond in English.

## Rules
1. Explain one concept at a time before moving on.
2. Use comparisons to everyday objects when describing scale.
3. If you are not sure about a number, say so honestly.

## Never Do
- Never invent fake astronomy facts.
...

## Vocabulary
Use these terms: telescope, constellation, nebula
Avoid these terms: astrology, horoscope

## Greeting
Open each conversation with this line: Hi! I am Nova. Ask me anything about the night sky.

## Examples
### Example 1

User: What's that bright star near the moon?

Assistant: That is most likely Venus, the brightest planet in our sky. ...
```

(Full text: `csharp/tests/golden_prompt.txt`. Sections with no content are omitted, so a
minimal persona renders as just `# Bot`.)

## Provider exports

| Method | Produces |
| --- | --- |
| `ToOpenAi(model)` / `to_openai(model)` | `{"model": ..., "messages": [{"role": "system", "content": prompt}]}` |
| `ToAnthropic(model, maxTokens)` / `to_anthropic(model, max_tokens)` | `{"model": ..., "max_tokens": ..., "system": prompt, "messages": []}` |
| `ToGemini()` / `to_gemini()` | `{"system_instruction": {"parts": [{"text": prompt}]}, "contents": []}` |

- **C# export methods return compact JSON strings** (pass straight to your HTTP client).
- **Python export methods return dicts** (pass as `json=` to `requests`/`httpx`).
- For Gemini, the model name goes in the REST URL (`models/{model}:generateContent`), not the body.
- `ToOpenAiMessages()` / `to_openai_messages()` returns typed `ChatMessage` records if you
  prefer to serialize yourself.

## JSON interchange

Personas serialize to a flat, versioned snake_case document (see
`examples/nova.persona.json`). Because both ports use the same key order, formatting and
null rules, the files are **byte-identical and freely interchangeable**: save in C#,
load in Python, and vice versa.

```csharp
persona.SaveTo("nova.persona.json");            // C#
var back = BuiltPersona.LoadFrom("nova.persona.json");
```

```python
persona.save_to("nova.persona.json")            # Python
back = BuiltPersona.load_from("nova.persona.json")
```

`schema_version` is bumped on incompatible format changes; loaders ignore unknown keys so
newer files still open in older code.

## Token budgeting

`TokenEstimator` / `talktoken.tokens` uses the common ~4-characters-per-token heuristic
for English text - good for budgeting, not for billing:

```csharp
TokenEstimator.Estimate("Hello, world!");            // 4
persona.FitsBudget(maxTokens: 4000);                 // rendered prompt vs context budget
```

```python
TokenEstimator.estimate("Hello, world!")             # 4
persona.fits_budget(max_tokens=4000)
```

For exact counts, plug the rendered prompt into a real tokenizer (e.g. tiktoken /
SharpToken) - TalkToken only needs `render_system_prompt()`.

## API map (C# <-> Python)

| Concept | C# | Python |
| --- | --- | --- |
| Start a persona | `AiPersona.Create("Nova")` | `AiPersona.create("Nova")` |
| Identity | `WithDescription` / `WithRole` / `WithBackground` | `with_description` / `with_role` / `with_background` |
| Voice | `WithTone` / `WithStyle` / `WithQuirk` / `WithVerbosity` | `with_tone` / `with_style` / `with_quirk` / `with_verbosity` |
| Behavior | `WithRules` / `AddRule` / `WithProhibitions` | `with_rules` / `add_rule` / `with_prohibitions` |
| Vocabulary | `WithPreferredTerms` / `WithBannedTerms` | `with_preferred_terms` / `with_banned_terms` |
| Facts & examples | `WithKnowledge` / `WithExample` / `WithExamples` | `with_knowledge` / `with_example` / `with_examples` |
| Extras | `WithLanguage` / `WithOutputFormat` / `WithGreeting` | `with_language` / `with_output_format` / `with_greeting` |
| Freeze | `Build()` | `build()` |
| Render | `RenderSystemPrompt()` | `render_system_prompt()` |
| Tokens | `EstimateTokens()` / `FitsBudget(n)` | `estimate_tokens()` / `fits_budget(n)` |
| Export | `ToOpenAi` / `ToAnthropic` / `ToGemini` (JSON strings) | `to_openai` / `to_anthropic` / `to_gemini` (dicts) |
| Persist | `ToJson` / `FromJson` / `SaveTo` / `LoadFrom` | `to_json` / `from_json` / `save_to` / `load_from` |

Behavior notes:

- All strings are trimmed at `Build()`; blank strings and blank list entries are dropped.
- Empty name on `Create`/`create` throws (`ArgumentException` / `ValueError`).
- `Verbosity.Balanced` emits no verbosity line (it is the default).
- Validation errors map to each language's idiomatic exception type.

## Testing

The two suites mirror each other case-for-case (28 tests each) and both compare the
sample persona against the same golden prompt file, which locks cross-language parity:

```bash
dotnet test csharp/TalkToken.sln                          # C#: 28 passed
PYTHONPATH=python/src python3 -m pytest python/tests -q   # Python: 28 passed
```

## License

MIT - see `LICENSE`.
