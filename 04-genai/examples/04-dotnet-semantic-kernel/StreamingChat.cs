using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GenAiExamples.SemanticKernel;

public sealed class StreamingChat
{
    private readonly IChatCompletionService _chatCompletion;
    private readonly Kernel _kernel;
    private readonly ILogger<StreamingChat> _logger;

    public StreamingChat(
        IChatCompletionService chatCompletion,
        Kernel kernel,
        ILogger<StreamingChat> logger)
    {
        _chatCompletion = chatCompletion;
        _kernel = kernel;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> StreamAnswerAsync(
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(
            "You are a concise fullstack GenAI assistant. " +
            "Prefer practical architecture and code examples.");
        history.AddUserMessage(message);

        await foreach (var chunk in _chatCompletion.GetStreamingChatMessageContentsAsync(
                           history,
                           kernel: _kernel,
                           cancellationToken: cancellationToken))
        {
            if (string.IsNullOrEmpty(chunk.Content))
            {
                continue;
            }

            yield return chunk.Content;
        }

        _logger.LogDebug("Completed streaming chat response");
    }
}

/*
Program.cs registration sketch:

using GenAiExamples.SemanticKernel;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IVectorSearch, InMemoryVectorSearch>();
builder.Services.AddScoped<RagService>();
builder.Services.AddScoped<StreamingChat>();

builder.Services.AddKernel()
    .AddOpenAIChatCompletion(
        modelId: builder.Configuration["AI:ChatModel"]!,
        apiKey: builder.Configuration["AI:ApiKey"]!);

var app = builder.Build();
app.MapControllers();
app.Run();
*/

