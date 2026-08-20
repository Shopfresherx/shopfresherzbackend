namespace ShopFresherz.Application.Dtos.Chatbot;

/// <summary>Request payload for sending a message to the AI chatbot.</summary>
public sealed class ChatMessageRequest
{
    /// <summary>Gets or sets the message content.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the conversation ID for context (optional).</summary>
    public string? ConversationId { get; set; }
}

/// <summary>Response from the AI chatbot.</summary>
public sealed class ChatMessageResponse
{
    /// <summary>Gets or sets the chatbot's reply.</summary>
    public string Reply { get; set; } = string.Empty;

    /// <summary>Gets or sets the conversation ID for subsequent messages.</summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>Gets or sets suggested quick replies.</summary>
    public string[]? QuickReplies { get; set; }
}