using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Chatbot;
using ShopFresherz.Application.Features.Chatbot.Commands;

namespace ShopFresherz.API.Controllers;

/// <summary>AI-powered customer support chatbot.</summary>
[ApiController]
[Route("api/v1/chatbot")]
public sealed class ChatbotController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatbotController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Sends a message to the AI chatbot and returns the response.</summary>
    [HttpPost("message")]
    [ProducesResponseType(typeof(ChatMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage(
        [FromBody] ChatMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { Code = "VALIDATION", Message = "Message is required." });
        }

        Result<ChatMessageResponse> result = await _mediator.Send(
            new SendChatMessageCommand(request.Message, request.ConversationId), 
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : StatusCode(500, result.Error);
    }
}