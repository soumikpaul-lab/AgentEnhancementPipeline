# Agent Enhancement Pipeline

Agent Enhancement Pipeline is a small .NET console application that demonstrates how to use the **Decorator design pattern** to enhance a base AI agent built with the Microsoft Agent framework.

The goal of this repository is not only to show a working agent, but to emphasize an important engineering habit: agentic coding still benefits from classic software design patterns. As AI applications grow, patterns such as Decorator help keep the codebase extensible, testable, observable, and easier to evolve.

## Featured Article

Want the story behind this repo? Read the companion blog post:

[Design Patterns Are Not Dead. Your AI Agent Just Forgot to Invite Them.](https://medium.com/@paulsoumik66/design-patterns-are-not-dead-your-ai-agent-just-forgot-to-invite-them-e872b323f463?sharedUserId=paulsoumik66)

## Why This Repository Exists

Agent applications often start simple: connect to an LLM, send a prompt, and return a response. Over time, real-world systems need more behavior around that base capability:

- token tracking
- logging
- telemetry
- policy checks
- caching
- retries
- guardrails
- cost monitoring
- tracing

Adding all of that directly into the base client or agent quickly creates tightly coupled code. This project shows a cleaner approach: wrap the base chat client with decorators that add behavior without changing the original implementation.

## What It Demonstrates

This repo demonstrates:

- Creating an `AIAgent` with `Microsoft.Agents.AI`
- Creating an OpenAI-compatible chat client
- Decorating the base `IChatClient` with `TokenTrackingChatClient`
- Tracking input, output, and total token usage
- Keeping cross-cutting concerns separate from the core agent behavior
- Using configuration, environment variables, and user secrets for local setup

## Decorator Pattern in This Project

The central example is `TokenTrackingChatClient`.

```csharp
public sealed class TokenTrackingChatClient(IChatClient innerChatClient, ILogger<TokenTrackingChatClient>? logger = null)
    : DelegatingChatClient(innerChatClient)
{
    // Adds token tracking while preserving the original IChatClient behavior.
}
```

`TokenTrackingChatClient` wraps an existing `IChatClient`. It delegates calls to the inner client, then reads usage metadata from the response and accumulates token counts.

This means the base client remains focused on communicating with the model, while the decorator handles observability.

```text
AIAgent
  |
  v
TokenTrackingChatClient
  |
  v
Base IChatClient
  |
  v
LLM Provider
```

## Repository Structure

```text
.
├── AgentEnhancementPipeline.sln
├── AgentEnhancementPipeline/
│   ├── AgentEnhancementPipeline.csproj
│   ├── Program.cs
│   ├── LlmClientFactory.cs
│   ├── TokenTrackingChatClient.cs
│   └── appsettings.json
└── README.md
```

### Key Files

| File | Purpose |
| --- | --- |
| `Program.cs` | Application entry point. Loads configuration, creates the agent, runs a sample prompt, and prints token metrics. |
| `LlmClientFactory.cs` | Creates the configured provider client, wraps it with the token-tracking decorator, and builds the agent. |
| `TokenTrackingChatClient.cs` | Decorator implementation that adds token tracking to any compatible `IChatClient`. |
| `appsettings.json` | Local configuration template for provider selection and OpenAI-compatible settings. |

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) or a compatible SDK for the target framework in the project
- An OpenAI-compatible endpoint
- An API key for your selected endpoint

You can confirm your installed SDK version with:

```bash
dotnet --version
```

## Configuration

The app reads configuration from the following sources:

1. `appsettings.json`
2. Environment variables
3. .NET user secrets

`appsettings.json` contains placeholders:

```json
{
  "LlmProvider": "OpenAi",
  "OpenAi": {
    "EndPoint": "",
    "ApiKey": ""
  }
}
```

For local development, prefer user secrets so credentials are not committed.

```bash
cd AgentEnhancementPipeline
dotnet user-secrets set "OpenAi:EndPoint" "https://your-endpoint.example.com"
dotnet user-secrets set "OpenAi:ApiKey" "your-api-key"
dotnet user-secrets set "OpenAi:Model" "your-model-name"
```

You can also use environment variables:

```bash
export OpenAi__EndPoint="https://your-endpoint.example.com"
export OpenAi__ApiKey="your-api-key"
export OpenAi__Model="your-model-name"
```

### Using LM Studio as a Local LLM Server

If you want to run this sample without sending requests to a hosted LLM provider, you can use [LM Studio](https://lmstudio.ai/) as a local OpenAI-compatible server. This is useful for local experimentation because, after downloading a model, requests can run on your own machine. That can help reduce hosted API cost concerns and keep prompts, responses, and documents local for stronger privacy and security.

LM Studio's official docs explain that it can serve local models from `localhost`, supports OpenAI-compatible endpoints, and can operate offline once model files are downloaded:

- [LM Studio local server setup](https://lmstudio.ai/docs/developer/core/server)
- [OpenAI-compatible endpoints](https://lmstudio.ai/docs/developer/openai-compat)
- [Download a local LLM in LM Studio](https://lmstudio.ai/docs/app/basics/download-model)
- [Offline operation, privacy, and local server behavior](https://lmstudio.ai/docs/app/offline)

Typical local setup:

1. Install LM Studio.
2. Download a model in LM Studio.
3. Start the local server from the Developer tab, or run:

```bash
lms server start
```

4. Configure this project to use the LM Studio OpenAI-compatible base URL:

```bash
cd AgentEnhancementPipeline
dotnet user-secrets set "OpenAi:EndPoint" "http://localhost:1234/v1"
dotnet user-secrets set "OpenAi:ApiKey" "lm-studio"
dotnet user-secrets set "OpenAi:Model" "your-local-model-id"
```

Use the model identifier shown in LM Studio for `OpenAi:Model`. LM Studio commonly runs the local server on port `1234`, but use the port shown in your LM Studio Developer tab if it is different.

## Run the Application

From the repository root:

```bash
dotnet restore
dotnet build
dotnet run --project AgentEnhancementPipeline/AgentEnhancementPipeline.csproj
```

The app sends a sample coding prompt to the agent:

```text
Write a Python function to sort a list.
```

After the response, it prints token usage collected by the decorator:

```text
Input Tokens - ...
Output Tokens - ...
Total Tokens - ...
```

## Design Pattern Benefits

Using the Decorator pattern gives this agent pipeline several benefits:

- **Open for extension, closed for modification**: Add new behavior without changing the base client.
- **Composable enhancements**: Add future decorators for logging, retries, cost controls, safety checks, or caching.
- **Separation of concerns**: Keep model communication separate from observability and operational behavior.
- **Provider flexibility**: Wrap any compatible `IChatClient` without coupling the decorator to a specific provider.
- **Better testing**: Test decorators independently from the LLM provider.

## Extending the Pipeline

You can add more decorators using the same approach:

```text
AIAgent
  |
  v
TelemetryChatClient
  |
  v
RetryChatClient
  |
  v
TokenTrackingChatClient
  |
  v
Base IChatClient
```

Possible future decorators:

- `RetryChatClient`
- `LoggingChatClient`
- `CachingChatClient`
- `CostLimitChatClient`
- `PolicyValidationChatClient`
- `PromptAuditChatClient`

Each decorator should focus on one concern and delegate the actual model call to the wrapped client.

## Best Practices Highlighted

- Treat agentic systems as software systems, not scripts.
- Keep infrastructure concerns out of core agent behavior.
- Prefer composition over inheritance for optional capabilities.
- Make observability a first-class part of the design.
- Store secrets outside source control.
- Design for extension before the system becomes hard to change.

## Project Status

This repository is intended as a focused educational sample. It is a good starting point for experimenting with composable agent enhancements using Microsoft Agent framework abstractions.

## License

This project is licensed under the [MIT License](LICENSE).
