using MediatR;
using ShopFresherz.Application.Common;
using ShopFresherz.Application.Dtos.Chatbot;
using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Application.Features.Chatbot.Commands;

/// <summary>Sends a message to the AI chatbot and returns the response.</summary>
public sealed record SendChatMessageCommand(string Message, string? ConversationId) 
    : IRequest<Result<ChatMessageResponse>>;

/// <summary>Handles AI chatbot message processing.</summary>
public sealed class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, Result<ChatMessageResponse>>
{
    private readonly IChatbotService _chatbotService;

    public SendChatMessageCommandHandler(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    public async Task<Result<ChatMessageResponse>> Handle(
        SendChatMessageCommand request, 
        CancellationToken cancellationToken)
    {
        var chatbotResponse = await _chatbotService.GetResponseAsync(
            request.Message, 
            request.ConversationId, 
            cancellationToken);

        var result = new ChatMessageResponse
        {
            Reply = chatbotResponse.Reply,
            ConversationId = chatbotResponse.ConversationId,
            QuickReplies = chatbotResponse.QuickReplies
        };

        return Result<ChatMessageResponse>.Success(result);
    }
}