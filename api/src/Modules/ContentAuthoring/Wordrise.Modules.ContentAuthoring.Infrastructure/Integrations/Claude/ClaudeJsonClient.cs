using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Integrations.Claude;

/// <summary>Raised when Claude cannot give a usable answer; the draft fails with this message.</summary>
internal sealed class ClaudeException(string message) : Exception(message);

/// <summary>Creates the Anthropic client the first time it is needed, so the app starts without a key.</summary>
internal sealed class ClaudeClientProvider(IOptions<AuthoringOptions> options)
{
    private readonly Lazy<AnthropicClient> _client = new(() =>
    {
        var key = options.Value.Claude.ApiKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ClaudeException(
                "Authoring:Claude:ApiKey is not set. Put the key in the environment variable " +
                "Authoring__Claude__ApiKey (or dotnet user-secrets), or set Authoring:Provider to Sample.");
        }

        var baseUrl = options.Value.Claude.BaseUrl;
        return string.IsNullOrWhiteSpace(baseUrl)
            ? new AnthropicClient { ApiKey = key }
            : new AnthropicClient { ApiKey = key, BaseUrl = baseUrl };
    });

    public AnthropicClient Client => _client.Value;
}

/// <summary>Asks Claude a question and gets back JSON that matches a schema.</summary>
internal sealed class ClaudeJsonClient(ClaudeClientProvider clients, ILogger<ClaudeJsonClient> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<T> AskAsync<T>(
        ClaudeModelOptions model,
        string system,
        string request,
        Dictionary<string, JsonElement> schema,
        CancellationToken cancellationToken)
    {
        var response = await clients.Client.Messages.Create(
            new MessageCreateParams
            {
                Model = model.Model,
                MaxTokens = model.MaxTokens,
                System = new List<TextBlockParam> { new() { Text = system } },
                Messages = [new UserMessageParam { Content = request }],
                // Opus 5.5 and Sonnet 5.5 always think; effort is how hard. No sampling settings: they are not accepted.
                OutputConfig = new OutputConfig
                {
                    Effort = ToEffort(model.Effort),
                    Format = new JsonOutputFormat { Schema = schema },
                },
            },
            cancellationToken);

        logger.LogInformation(
            "Claude {Model}: {InputTokens} tokens in, {OutputTokens} out, stopped by {StopReason}",
            model.Model,
            response.Usage.InputTokens,
            response.Usage.OutputTokens,
            response.StopReason);

        if (response.StopReason == "refusal")
        {
            throw new ClaudeException($"{model.Model} declined to answer this request.");
        }

        if (response.StopReason == "max_tokens")
        {
            throw new ClaudeException(
                $"{model.Model} ran out of tokens before finishing its answer. Raise Authoring:Claude MaxTokens for this role.");
        }

        var text = response.Content.Select(block => block.Value).OfType<TextBlock>().FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ClaudeException($"{model.Model} returned no text.");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(text, Json)
                ?? throw new ClaudeException($"{model.Model} returned an empty answer.");
        }
        catch (JsonException exception)
        {
            throw new ClaudeException($"{model.Model} returned an answer that is not the expected JSON: {exception.Message}");
        }
    }

    private static Effort ToEffort(ClaudeEffort effort) => effort switch
    {
        ClaudeEffort.Low => Effort.Low,
        ClaudeEffort.Medium => Effort.Medium,
        ClaudeEffort.High => Effort.High,
        ClaudeEffort.Max => Effort.Max,
        _ => throw new ArgumentOutOfRangeException(nameof(effort), effort, null),
    };
}
