namespace TalkToken;

/// <summary>One user/assistant turn pair used as a few-shot example inside the prompt.</summary>
public sealed record DialogueExample(string User, string Assistant);

/// <summary>A single chat message (role + content), e.g. as returned by <see cref="BuiltPersona.ToOpenAiMessages"/>.</summary>
public sealed record ChatMessage(string Role, string Content);
