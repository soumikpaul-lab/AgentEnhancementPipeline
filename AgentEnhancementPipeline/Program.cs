using System.ClientModel;
using AgentEnhancementPipeline;
using OpenAI;
using OpenAI.Responses;
using Microsoft.Extensions.AI;


var options = new OpenAIClientOptions()
{
    Endpoint =  new Uri("http://127.0.0.1:1234/v1"),
};
var apiKey = "sk-lm-X1EwED5X:Qh271SmcWwcw5Dg5MAod";
#pragma  warning disable OPENAI001,MAAI001 // Type or member is obsolete
var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey), options);
var chatClient = new TokenTrackingChatClient(openAiClient.GetResponsesClient().AsIChatClientWithStoredOutputDisabled("google/gemma-4-e4b"));
var agent = chatClient.AsAIAgent(
    instructions: "You are a helpful coding assistant.",
    name:"CodeHelper");

Console.WriteLine(await agent.RunAsync("Write a Python function to sort a list."));
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"Input Tokens - {chatClient.InputTokens}");
Console.WriteLine($"Output Tokens - {chatClient.OutputTokens}");
Console.WriteLine($"Total Tokens - {chatClient.TotalTokens}");

