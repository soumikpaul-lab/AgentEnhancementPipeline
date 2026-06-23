using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgentEnhancementPipeline;

public sealed class TokenTrackingChatClient(IChatClient innerChatClient, ILogger<TokenTrackingChatClient> logger = null)
    : DelegatingChatClient(innerChatClient)
{
    private long _inputTokens;
    private long _outputTokens;

    public long InputTokens => Interlocked.Read(ref _inputTokens);
    public long OutputTokens => Interlocked.Read(ref _outputTokens);
    
    public long TotalTokens => InputTokens + OutputTokens;

    /// <summary>
    /// Overriding the response with token usage
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var response = await base.GetResponseAsync(messages, options, cancellationToken)
            .ConfigureAwait(false);

        if (response.Usage is null)
        {
            logger?.LogWarning("Token Tracking ChatClient response usage returned null. Check configuration");
        }
        AccumulateUsage(response.Usage);
        return response;
        
    }

    /// <summary>
    /// Overriding the streaming response to add token usage
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = new CancellationToken())
    {
        var usageCaptured = false;

        await foreach (ChatResponseUpdate update in base
                           .GetStreamingResponseAsync(messages, options, cancellationToken)
                           .ConfigureAwait(false)
                      )
        {
            //Providers emits a UsageCount on (usually) the last update
            foreach (UsageContent uc in update.Contents.OfType<UsageContent>())
            {
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
    
    private void AccumulateUsage(UsageDetails? usage)
    {
        if (usage is null) return;
        Interlocked.Add(ref _inputTokens,usage.InputTokenCount ?? 0);
        Interlocked.Add(ref _outputTokens,usage.OutputTokenCount ?? 0);
            
    }
}