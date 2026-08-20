using ShopFresherz.Domain.Interfaces.Services;

namespace ShopFresherz.Infrastructure.Services;

/// <summary>In-memory chatbot service implementation.</summary>
/// <remarks>This is a placeholder implementation. Replace with actual AI service integration.</remarks>
public sealed class ChatbotService : IChatbotService
{
    private readonly string[] _quickReplyOptions = 
    [
        "Track my order",
        "Product recommendations",
        "Return policy",
        "Contact support"
    ];

    public Task<ChatbotResponse> GetResponseAsync(
        string message, 
        string? conversationId, 
        CancellationToken cancellationToken = default)
    {
        var reply = GetChatbotReply(message);
        
        var response = new ChatbotResponse(
            Reply: reply,
            ConversationId: conversationId ?? Guid.NewGuid().ToString("N"),
            QuickReplies: _quickReplyOptions);

        return Task.FromResult(response);
    }

    private static string GetChatbotReply(string message)
    {
        var lowerMessage = message.ToLowerInvariant();

        return lowerMessage switch
        {
            var m when m.Contains("order") || m.Contains("track") =>
                "I can help you track your order. Please provide your order number.",
            var m when m.Contains("return") =>
                "Our return policy allows returns within 7 days of delivery. Items must be unused and in original packaging.",
            var m when m.Contains("price") || m.Contains("cost") =>
                "You can check our product prices on the website. Is there a specific product you're interested in?",
            var m when m.Contains("shipping") || m.Contains("delivery") =>
                "We offer nationwide delivery. Standard delivery takes 1-2 business days.",
            var m when m.Contains("hello") || m.Contains("hi") =>
                "Hello! I'm ShopFresherz assistant. How can I help you today?",
            _ => "I'm here to help! You can ask me about orders, returns, shipping, or products."
        };
    }
}