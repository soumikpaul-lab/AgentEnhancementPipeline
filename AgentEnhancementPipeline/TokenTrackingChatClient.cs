using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgentEnhancementPipeline;

/// <summary>
/// Decorator Pattern: Wraps an inner IChatClient to transparently track token usage
/// without modifying the inner client's behavior or interface.
/// The decorator intercepts GetResponseAsync and GetStreamingResponseAsync,
/// delegates to the inner client, then enriches the response with token metrics.
/// </summary>
public sealed class TokenTrackingChatClient(IChatClient innerChatClient, ILogger<TokenTrackingChatClient>? logger = null)
    // Decorator Pattern: Extends DelegatingChatClient to delegate all IChatClient calls
    // to the wrapped 'innerChatClient', while overriding specific methods to add
    // cross-cutting token tracking concerns.
    : DelegatingChatClient(innerChatClient)
{
    private long _inputTokens;
    private long _outputTokens;

    public long InputTokens => Interlocked.Read(ref _inputTokens);
    public long OutputTokens => Interlocked.Read(ref _outputTokens);

    public long TotalTokens => InputTokens + OutputTokens;

    /// <summary>
    /// Decorator Pattern: Intercepts the inner client's GetResponseAsync call,
    /// delegates to base (which forwards to innerChatClient), then extracts
    /// and accumulates token usage from the response — adding observability
    /// without altering the inner client's core logic.
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        // Decorator Pattern: Delegates to the inner client via base (DelegatingChatClient),
        // then enriches the result with token tracking — the core decoration step.
        var response = await base.GetResponseAsync(messages, options, cancellationToken)
            .ConfigureAwait(false);

        if (response.Usage is null)
        {
            logger?.LogWarning("Token Tracking ChatClient response usage returned null. Check configuration");
        }
        // Decorator Pattern: Post-processes the inner client's response to accumulate
        // token metrics, adding cross-cutting observability transparently.
        AccumulateUsage(response.Usage);
        return response;

    }

    /// <summary>
    /// Decorator Pattern: Intercepts the inner client's GetStreamingResponseAsync call,
    /// delegates to base (which forwards to innerChatClient), then extracts
    /// and accumulates token usage from each streaming update — adding observability
    /// to streaming responses without modifying the inner client.
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = new CancellationToken())
    {
        var usageCaptured = false;

        // Decorator Pattern: Delegates to the inner client via base (DelegatingChatClient),
        // iterating over each streaming update to extract token usage — the decoration
        // step that adds observability to the streaming pipeline.
        await foreach (ChatResponseUpdate update in base
                           .GetStreamingResponseAsync(messages, options, cancellationToken)
                           .ConfigureAwait(false)
                      )
        {
            // Providers emits a UsageCount on (usually) the last update
            foreach (UsageContent uc in update.Contents.OfType<UsageContent>())
            {
                // Decorator Pattern: Post-processes each streaming update to accumulate
                // token metrics, adding cross-cutting observability transparently.
                AccumulateUsage(uc.Details);
                usageCaptured = true;
            }
            yield return update;
        }

        if (!usageCaptured)
        {
            logger?.LogWarning("Token Tracking ChatClient usage returned null. Check configuration. Or provider may not emit usage in streaming mode");
        }
    }

    /// <summary>
    /// Accumulates token usage from the inner client's response into thread-safe counters.
    /// This is the core state mutation of the decorator — adding observability
    /// without affecting the inner client's behavior.
    /// </summary>
    private void AccumulateUsage(UsageDetails? usage)
    {
        if (usage is null) return;
        Interlocked.Add(ref _inputTokens, usage.InputTokenCount ?? 0);
        Interlocked.Add(ref _outputTokens, usage.OutputTokenCount ?? 0);

    }
}