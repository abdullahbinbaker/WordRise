using System.Text.Json;
using System.Text.Json.Nodes;
using Wordrise.Modules.ContentAuthoring.Domain;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Integrations.Claude;

/// <summary>
/// The JSON schemas of Claude's answers. Structured outputs guarantee the reply follows them,
/// so the code never has to parse free text. Only the keywords structured outputs support are
/// used: every object lists all its fields as required and allows no others.
/// </summary>
internal static class ClaudeSchemas
{
    public static Dictionary<string, JsonElement> Lesson { get; } = ToDictionary(
        Obj(
            ("title", Str()),
            ("words", Arr(Obj(
                ("target", Str()),
                ("native", Str()),
                ("pronunciation", Str()),
                ("example", Str()),
                ("exampleNative", Str())))),
            ("dialogue", Arr(Obj(
                ("speaker", Str()),
                ("isLearner", Bool()),
                ("target", Str()),
                ("native", Str())))),
            ("chatPrompt", Str()),
            ("chatOptions", Arr(Option())),
            ("grammar", Obj(
                ("title", Str()),
                ("meaning", Str()),
                ("explanation", Str()),
                ("pattern", Str()),
                ("examples", Arr(Obj(("target", Str()), ("native", Str())))),
                ("note", Str()))),
            ("listeningSentence", Str()),
            ("listeningOptions", Arr(Option()))));

    public static Dictionary<string, JsonElement> Review { get; } = ToDictionary(
        Obj(
            ("summary", Str()),
            ("criteria", Arr(Obj(
                ("criterion", EnumOf([.. Enum.GetNames<ReviewCriterion>()])),
                ("passed", Bool()),
                ("comment", Str())))),
            ("issues", Arr(Obj(
                ("severity", EnumOf("blocking", "suggestion")),
                ("where", Str()),
                ("problem", Str()),
                ("suggestion", Str()))))));

    private static JsonObject Option() => Obj(("text", Str()), ("isCorrect", Bool()));

    private static JsonObject Obj(params (string Name, JsonNode Schema)[] properties) => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject(properties.Select(p => KeyValuePair.Create(p.Name, (JsonNode?)p.Schema))),
        ["required"] = new JsonArray([.. properties.Select(p => (JsonNode)p.Name)]),
        ["additionalProperties"] = false,
    };

    private static JsonObject Str() => new() { ["type"] = "string" };

    private static JsonObject Bool() => new() { ["type"] = "boolean" };

    private static JsonObject Arr(JsonNode items) => new() { ["type"] = "array", ["items"] = items };

    private static JsonObject EnumOf(params string[] values) => new()
    {
        ["type"] = "string",
        ["enum"] = new JsonArray([.. values.Select(v => (JsonNode)v)]),
    };

    /// <summary>The SDK takes the schema as its top-level keywords.</summary>
    private static Dictionary<string, JsonElement> ToDictionary(JsonObject schema) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(schema.ToJsonString())!;
}
