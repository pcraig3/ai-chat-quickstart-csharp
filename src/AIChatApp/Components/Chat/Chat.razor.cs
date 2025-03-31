using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AIChatApp.Model;
using AIChatApp.Services;

namespace AIChatApp.Components.Chat;

public partial class Chat
{
    [Inject]
    internal ChatService? ChatHandler { get; init; }
    [Inject]
    internal SearchService? SearchHandler { get; init; }
    List<Message> messages = new();
    ElementReference writeMessageElement;
    string? userMessageText;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await using var module = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Chat/Chat.razor.js");
                await module.InvokeVoidAsync("submitOnEnter", writeMessageElement);
                await module.InvokeVoidAsync("autoResizeTextarea", "expanding-textarea", 3, 5);
            }
            catch (JSDisconnectedException)
            {
                // Not an error
            }
        }
    }

    async void SendMessage()
    {
        if (ChatHandler is null || SearchHandler is null) return;

        if (!string.IsNullOrWhiteSpace(userMessageText))
        {
            var docs = await SearchHandler.GetTopChunks(userMessageText);
            string context = string.Join("\n", docs);
            string prompt = "You are a helpful assistant who answers submitted questions based on the context contained in the triple backticks. If you don't know the answer, just say \"I don't know, that's not in Paul's CV.\" Don't try to make up an answer.";

            string finalPrompt = $@"
            {prompt}

            Context:
            ```
            {context}
            ```

            Question: {userMessageText}
            Helpful Answer:
            ";

            // Show user's original message in the chat UI
            messages.Add(new Message()
            {
                IsAssistant = false,
                Content = userMessageText
            });

            // clear message
            userMessageText = null;

            // Create a temporary assistant message in the UI
            Message assistantMessage = new Message()
            {
                IsAssistant = true,
                Content = ""
            };
            messages.Add(assistantMessage);
            StateHasChanged();

            // Send ONLY the RAG-augmented prompt to the model
            var modelMessages = new List<Message>
            {
                new Message()
                {
                    IsAssistant = false,
                    Content = finalPrompt
                }
            };
            ChatRequest request = new ChatRequest(modelMessages);

            IAsyncEnumerable<string> chunks = ChatHandler.Stream(request);

            await foreach (var chunk in chunks)
            {
                assistantMessage.Content += chunk;
                StateHasChanged();
            }
        }
    }
}