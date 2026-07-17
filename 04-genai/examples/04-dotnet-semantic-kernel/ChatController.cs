using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace GenAiExamples.SemanticKernel;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly RagService _ragService;
    private readonly StreamingChat _streamingChat;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        RagService ragService,
        StreamingChat streamingChat,
        ILogger<ChatController> logger)
    {
        _ragService = ragService;
        _streamingChat = streamingChat;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> AskAsync(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var tenantId = User.FindFirst("tenant_id")?.Value ?? request.TenantId;
        var result = await _ragService.AnswerAsync(
            question: request.Message,
            tenantId: tenantId,
            cancellationToken: cancellationToken);

        return Ok(new ChatResponse(
            Answer: result.Answer,
            Citations: result.Citations
                .Select(citation => new CitationDto(
                    citation.Id,
                    citation.Title,
                    citation.Url,
                    citation.Score))
                .ToArray()));
    }

    [HttpPost("stream")]
    public async Task StreamAsync(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("Message is required.", cancellationToken);
            return;
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";

        await foreach (var token in _streamingChat.StreamAnswerAsync(
                           request.Message,
                           cancellationToken))
        {
            await WriteSseAsync("delta", new { token }, cancellationToken);
        }

        await WriteSseAsync("done", new { }, cancellationToken);
    }

    private async Task WriteSseAsync(string eventName, object payload, CancellationToken cancellationToken)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        _logger.LogDebug("Wrote SSE event {EventName}", eventName);
    }
}

public sealed record ChatRequest(
    [property: Required, MinLength(1), MaxLength(8_000)] string Message,
    [property: Required, MinLength(1), MaxLength(100)] string TenantId);

public sealed record ChatResponse(
    string Answer,
    IReadOnlyList<CitationDto> Citations);

public sealed record CitationDto(
    string Id,
    string Title,
    string Url,
    double Score);

