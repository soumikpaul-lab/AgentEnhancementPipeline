using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;

namespace AgentEnhancementPipeline;

/// <summary>
/// Supported LLM providers. Extensible — add new providers here and handle them in LlmClientFactory.
/// </summary>
public enum LlmProvider
{
    OpenAi,
}

/// <summary>
/// Configuration model for OpenAI-compatible providers.
/// Properties are bound from the "OpenAi" section in appsettings.json (or overridden by environment variables / user secrets).
/// Model defaults to "google/gemma-4-e4b" when not specified in config.
/// </summary>
public class OpenAiConfig
{
    public static string SectionName => "OpenAi";
    public string EndPoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "google/gemma-4-e4b";
}

/// <summary>
/// Factory that creates a configured AIAgent and its underlying IChatClient for a given provider.
/// 
/// Pipeline (OpenAI path):
///   1. Reads provider-specific configuration from IConfigurationRoot.
///   2. Creates the raw OpenAIClient with credentials and endpoint.
///   3. Wraps the chat client with TokenTrackingChatClient to collect token usage metrics.
///   4. Builds an AIAgent with system instructions, OpenTelemetry middleware, and the wrapped client.
/// 
/// Returns both the agent (for high-level interactions) and the chat client (for accessing token metrics).
/// </summary>
public static class LlmClientFactory
{
    /// <summary>
    /// Creates a configured AIAgent and its underlying IChatClient for the specified provider.
    /// </summary>
    /// <param name="provider">The LLM provider to use (e.g., OpenAI).</param>
    /// <param name="configurationRoot">Configuration source containing provider settings.</param>
    /// <returns>A tuple of (AIAgent, IChatClient). The chat client can be cast to TokenTrackingChatClient to read token metrics.</returns>
    public static (AIAgent, IChatClient) CreateAgent(LlmProvider provider, IConfigurationRoot configurationRoot)
    {
        switch (provider)
        {
            case LlmProvider.OpenAi:
                // Step 1: Load provider-specific configuration from the "OpenAi" config section.
                // Throws if required settings (EndPoint, ApiKey) are missing.
                var openAiConfig = configurationRoot.GetSection(OpenAiConfig.SectionName).Get<OpenAiConfig>()
                    ?? throw new InvalidOperationException($"Missing {OpenAiConfig.SectionName} configuration.");

                // Step 2: Configure the OpenAIClient with the endpoint and API key.
                var options = new OpenAIClientOptions()
                {
                    Endpoint = new Uri(openAiConfig.EndPoint),
                };
#pragma warning disable OPENAI001, MAAI001 // Type or member is obsolete
                // Step 3: Create the raw OpenAIClient with credentials.
                var openAiClient = new OpenAIClient(new ApiKeyCredential(openAiConfig.ApiKey), options);

                // Step 4: Wrap the chat client with TokenTrackingChatClient to collect token usage metrics.
                // The inner client is obtained from OpenAIClient's Responses API, converted to IChatClient.
                var chatClient = new TokenTrackingChatClient(openAiClient.GetResponsesClient().AsIChatClientWithStoredOutputDisabled(model: openAiConfig.Model));

                // Step 5: Build the AIAgent with system instructions, name, OpenTelemetry middleware, and the wrapped chat client.
                var agent = chatClient.AsAIAgent(
                    instructions: "You are a helpful coding assistant.",
                    name: "CodeHelper").AsBuilder()
                    // Adds OpenTelemetry middleware for distributed tracing and observability.
                    .UseOpenTelemetry(sourceName: "AgentEnhancementPipeline")
                    .Build();

                // Return both the agent (for high-level agent interactions) and the chat client (for accessing token metrics).
                return (agent, chatClient);

            default:
                throw new NotSupportedException($"The provider {provider} is not supported.");
        }
    }

}