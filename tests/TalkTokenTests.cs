using System.Text.Json;
using TalkToken;
using Xunit;

namespace TalkToken.Tests;

public class RenderTests
{
    [Fact]
    public void HeaderOnly_WhenNothingElseIsSet() =>
        Assert.Equal("# Nova", AiPersona.Create("Nova").Build().RenderSystemPrompt());

    [Fact]
    public void Name_IsTrimmed() =>
        Assert.Equal("# Nova", AiPersona.Create("  Nova  ").Build().RenderSystemPrompt());

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_Throws(string name) =>
        Assert.Throws<ArgumentException>(() => AiPersona.Create(name));

    [Fact]
    public void BlankEntries_AreDropped_AndStringsTrimmed()
    {
        var persona = AiPersona.Create("Bot")
            .WithRules("  ", "  rule one  ", "")
            .WithPreferredTerms("", "  nebula ")
            .Build();

        Assert.Equal(["rule one"], persona.Rules);
        Assert.Equal(["nebula"], persona.PreferredTerms);
    }

    [Fact]
    public void FluentCalls_ReturnSameBuilder()
    {
        var builder = AiPersona.Create("Bot");
        Assert.Same(builder, builder.WithRole("r").AddRule("x").WithVerbosity(Verbosity.Terse));
    }

    [Fact]
    public void Sections_AreOmitted_WhenEmpty()
    {
        var prompt = AiPersona.Create("Bot").WithTone("calm").Build().RenderSystemPrompt();
        Assert.Equal("# Bot\n\n## Persona & Tone\n- Tone: calm", prompt);
    }

    [Fact]
    public void Rules_AreNumbered()
    {
        var prompt = AiPersona.Create("Bot").WithRules("be kind", "be brief").Build().RenderSystemPrompt();
        Assert.Contains("## Rules\n1. be kind\n2. be brief", prompt);
    }

    [Fact]
    public void BalancedVerbosity_IsOmitted_TerseIsIncluded()
    {
        var balanced = AiPersona.Create("Bot").Build().RenderSystemPrompt();
        Assert.DoesNotContain("Verbosity", balanced);

        var terse = AiPersona.Create("Bot").WithVerbosity(Verbosity.Terse).Build().RenderSystemPrompt();
        Assert.Contains("- Verbosity: Answer in as few words as possible.", terse);
    }

    [Fact]
    public void Vocabulary_ShowsOnlyWhatWasSet()
    {
        var prompt = AiPersona.Create("Bot").WithPreferredTerms("scalar", "vector").Build().RenderSystemPrompt();
        Assert.Contains("Use these terms: scalar, vector", prompt);
        Assert.DoesNotContain("Avoid these terms", prompt);
    }

    [Fact]
    public void Examples_AreNumbered_InOrder()
    {
        var prompt = AiPersona.Create("Bot")
            .WithExample("hi", "hello")
            .WithExample("bye", "goodbye")
            .Build()
            .RenderSystemPrompt();

        Assert.Contains("### Example 1\n\nUser: hi\n\nAssistant: hello", prompt);
        Assert.Contains("### Example 2\n\nUser: bye\n\nAssistant: goodbye", prompt);
    }

    [Fact]
    public void SamplePersona_MatchesGoldenPrompt()
    {
        var golden = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "golden_prompt.txt"))
            .TrimEnd('\r', '\n');
        Assert.Equal(golden, SamplePersonas.Nova().RenderSystemPrompt());
    }
}

public class TokenEstimatorTests
{
    [Theory]
    [InlineData("Hello, world!", 4)] // 13 chars -> ceil(13/4) = 4
    [InlineData("abcdefgh", 2)]      // 8 chars  -> 2
    [InlineData("abcdefg", 2)]       // 7 chars  -> ceil(1.75) = 2
    [InlineData("", 0)]
    public void Estimate_UsesFourCharsPerToken(string text, int expected) =>
        Assert.Equal(expected, TokenEstimator.Estimate(text));

    [Fact]
    public void Estimate_Null_IsZero() => Assert.Equal(0, TokenEstimator.Estimate(null));

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void Estimate_InvalidCharsPerToken_Throws(double charsPerToken) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => TokenEstimator.Estimate("abc", charsPerToken));

    [Fact]
    public void FitsBudget_ChecksRenderedPrompt()
    {
        var persona = SamplePersonas.Nova();
        Assert.True(persona.FitsBudget(persona.EstimateTokens()));
        Assert.False(persona.FitsBudget(persona.EstimateTokens() - 1));
    }
}

public class JsonTests
{
    [Fact]
    public void RoundTrip_PreservesEverything()
    {
        var persona = SamplePersonas.Nova();
        var back = BuiltPersona.FromJson(persona.ToJson());

        Assert.Equal(persona.RenderSystemPrompt(), back.RenderSystemPrompt());
        Assert.Equal(persona.Rules, back.Rules);
        Assert.Equal(persona.Prohibitions, back.Prohibitions);
        Assert.Equal(persona.Examples, back.Examples);
        Assert.Equal(persona.Verbosity, back.Verbosity);
        Assert.Equal(persona.PreferredTerms, back.PreferredTerms);
    }

    [Fact]
    public void Json_IsSnakeCase_AndVersioned()
    {
        var json = SamplePersonas.Nova().ToJson();
        Assert.Contains("\"schema_version\": 1", json);
        Assert.Contains("\"preferred_terms\"", json);
        Assert.Contains("\"output_format\"", json);
        Assert.Contains("\"verbosity\": \"concise\"", json);
        Assert.DoesNotContain("OutputFormat", json);
    }

    [Fact]
    public void MissingName_Throws() =>
        Assert.Throws<ArgumentException>(() => BuiltPersona.FromJson("{}"));

    [Fact]
    public void SaveAndLoad_RoundTripsThroughFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "talktoken-tests-nova.json");
        var persona = SamplePersonas.Nova();
        persona.SaveTo(path);
        try
        {
            Assert.Equal(persona.RenderSystemPrompt(), BuiltPersona.LoadFrom(path).RenderSystemPrompt());
        }
        finally
        {
            File.Delete(path);
        }
    }
}

public class ProviderExportTests
{
    private static readonly string Prompt = SamplePersonas.Nova().RenderSystemPrompt();

    [Fact]
    public void OpenAi_PayloadShape()
    {
        using var doc = JsonDocument.Parse(SamplePersonas.Nova().ToOpenAi());
        var root = doc.RootElement;
        Assert.False(root.TryGetProperty("model", out _));
        var messages = root.GetProperty("messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal(Prompt, messages[0].GetProperty("content").GetString());

        using var withModel = JsonDocument.Parse(SamplePersonas.Nova().ToOpenAi("gpt-4o-mini"));
        Assert.Equal("gpt-4o-mini", withModel.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public void Anthropic_PayloadShape()
    {
        using var doc = JsonDocument.Parse(SamplePersonas.Nova().ToAnthropic("claude-sonnet-4-5", 2048));
        var root = doc.RootElement;
        Assert.Equal("claude-sonnet-4-5", root.GetProperty("model").GetString());
        Assert.Equal(2048, root.GetProperty("max_tokens").GetInt32());
        Assert.Equal(Prompt, root.GetProperty("system").GetString());
        Assert.Equal(0, root.GetProperty("messages").GetArrayLength());
    }

    [Fact]
    public void Gemini_PayloadShape()
    {
        using var doc = JsonDocument.Parse(SamplePersonas.Nova().ToGemini());
        var root = doc.RootElement;
        var parts = root.GetProperty("system_instruction").GetProperty("parts");
        Assert.Equal(1, parts.GetArrayLength());
        Assert.Equal(Prompt, parts[0].GetProperty("text").GetString());
        Assert.Equal(0, root.GetProperty("contents").GetArrayLength());
    }

    [Fact]
    public void OpenAiMessages_ExposeChatMessages()
    {
        var messages = SamplePersonas.Nova().ToOpenAiMessages();
        var message = Assert.Single(messages);
        Assert.Equal("system", message.Role);
        Assert.Equal(Prompt, message.Content);
    }
}
