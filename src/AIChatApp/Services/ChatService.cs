using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using AIChatApp.Model;

namespace AIChatApp.Services;

internal class ChatService(IChatCompletionService chatService)
{
    internal async Task<Message> Chat(ChatRequest request)
    {
        ChatHistory history = CreateHistoryFromRequest(request);

        ChatMessageContent response = await chatService.GetChatMessageContentAsync(history);

        var textContent = response.Items[0] as TextContent;
        if (textContent is null || string.IsNullOrEmpty(textContent.Text))
        {
            throw new InvalidOperationException("Invalid or empty text content.");
        }

        return new Message()
        {
            IsAssistant = response.Role == AuthorRole.Assistant,
            Content = textContent.Text
        };
    }

    internal async IAsyncEnumerable<string> Stream(ChatRequest request)
    {
        ChatHistory history = CreateHistoryFromRequest(request);

        IAsyncEnumerable<StreamingChatMessageContent> response = chatService.GetStreamingChatMessageContentsAsync(history);

        await foreach (StreamingChatMessageContent content in response)
        {
            if (!string.IsNullOrEmpty(content.Content))
            {
                yield return content.Content;
            }
        }
    }

    private static ChatHistory CreateHistoryFromRequest(ChatRequest request)
    {
        ChatHistory history = new ChatHistory("You are a helpful assistant.");
        foreach (Message message in request.Messages)
        {
            if (message.IsAssistant)
            {
                history.AddAssistantMessage(message.Content);
            }
            else
            {
                history.AddUserMessage(message.Content);
            }
        }

        return history;
    }
}