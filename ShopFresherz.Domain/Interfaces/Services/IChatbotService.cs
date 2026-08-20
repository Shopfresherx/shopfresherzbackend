namespace ShopFresherz.Domain.Interfaces.Services;

/// <summary>Contract for AI chatbot message processing.</summary>
public interface IChatbotService
{
    /// <summary>Gets a response from the AI chatbot.</summary>
    Task<ChatbotResponse> GetResponseAsync(
        string message, 
        string? conversationId, 
        CancellationToken cancellationToken = default);
}

/// <summary>Chatbot response model.</summary>
public sealed record ChatbotResponse(
    string Reply,
    string ConversationId,
    string[]? QuickReplies = null);