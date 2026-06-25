using AgentEnhancementPipeline;
using Microsoft.Extensions.Configuration;

// Build configuration from multiple sources in priority order:
// 1. appsettings.json (base config)
// 2. Environment variables (overrides JSON)
// 3. User secrets (local development overrides, never committed)
IConfigurationRoot config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .Build();

// Resolve the LLM provider from config, defaulting to OpenAI if missing or invalid.
if (!Enum.TryParse(config.GetSection("LlmProvider").Value, out LlmProvider provider))
{
    provider = LlmProvider.OpenAi;
}

// Create the agent and chat client using the factory.
// The factory: loads provider config → creates the LLM client → wraps it with token tracking → builds the agent with instructions and telemetry.
var (agent, chatClient) = LlmClientFactory.CreateAgent(provider, config);

// Execute the agent with a sample prompt.
Console.WriteLine(await agent.RunAsync("Write a Python function to sort a list."));

// Cast back to TokenTrackingChatClient to access token metrics tracked during execution.
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"Input Tokens - {(chatClient as TokenTrackingChatClient)?.InputTokens}");
Console.WriteLine($"Output Tokens - {(chatClient as TokenTrackingChatClient)?.OutputTokens}");
Console.WriteLine($"Total Tokens - {(chatClient as TokenTrackingChatClient)?.TotalTokens}");

