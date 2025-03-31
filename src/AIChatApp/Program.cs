using AIChatApp.Components;
using AIChatApp.Model;
using AIChatApp.Services;
using Azure.Identity;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration.Json;

var builder = WebApplication.CreateBuilder(args);

// Add support for a local configuration file, which doesn't get committed to source control
builder.Configuration.Sources.Insert(0, new JsonConfigurationSource { Path = "appsettings.Local.json", Optional = true });

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure AI related features
builder.Services.AddKernel();

builder.Services.AddHttpClient<SearchService>();

var aiHost = builder.Configuration["AIHost"];
if (String.IsNullOrEmpty(aiHost))
{
    aiHost = "OpenAI";
}

switch (aiHost)
{
    case "github":
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        builder.Services.AddAzureAIInferenceChatCompletion(
            modelId: builder.Configuration["GITHUB_MODEL_NAME"],
            builder.Configuration["GITHUB_TOKEN"],
            new Uri("https://models.inference.ai.azure.com"));
        break;
    case "azureAIModelCatalog":
        builder.Services.AddAzureAIInferenceChatCompletion(
            builder.Configuration["AZURE_MODEL_NAME"],
            builder.Configuration["AZURE_INFERENCE_KEY"],
            new Uri(builder.Configuration["AZURE_MODEL_ENDPOINT"]!));
        break;
    case "local":
        var localModelName = builder.Configuration["LOCAL_MODEL_NAME"];
        var localEndpoint = builder.Configuration["LOCAL_ENDPOINT"];

        if (string.IsNullOrEmpty(localModelName) || string.IsNullOrEmpty(localEndpoint))
            throw new InvalidOperationException("LOCAL_MODEL_NAME or LOCAL_ENDPOINT is not set in configuration.");

        builder.Services.AddOllamaChatCompletion(
            modelId: localModelName,
            endpoint: new Uri(localEndpoint));
        break;
    default:

        var azureOpenAiDeployment = builder.Configuration["AZURE_OPENAI_DEPLOYMENT"];
        var azureOpenAiEndpoint = builder.Configuration["AZURE_OPENAI_ENDPOINT"];

        if (string.IsNullOrEmpty(azureOpenAiDeployment) || string.IsNullOrEmpty(azureOpenAiEndpoint))
            throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT or AZURE_OPENAI_ENDPOINT is not set in configuration.");

        builder.Services.AddAzureOpenAIChatCompletion(azureOpenAiDeployment, azureOpenAiEndpoint, new DefaultAzureCredential());
        break;
}

builder.Services.AddSingleton<ChatService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

// Configure APIs for chat related features
//app.MapPost("/chat", (ChatRequest request, ChatHandler chatHandler) => (chatHandler.); // Uncomment for a non-streaming response
app.MapPost("/chat/stream", (ChatRequest request, ChatService chatHandler) => chatHandler.Stream(request));

app.Run();