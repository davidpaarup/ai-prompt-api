using System.Net.WebSockets;
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelApi;
using SemanticKernelApi.Plugins;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<GraphClientFactory>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://ai-prompt-bice.vercel.app")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseWebSockets();
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var modelId = app.Configuration["ModelId"];
var apiKey = app.Configuration["OpenAIKey"];

if (modelId == null || apiKey == null)
{
    throw new Exception("OpenAI configuration missing");
}

var kernelBuilder = Kernel.CreateBuilder().AddOpenAIChatCompletion(modelId, apiKey);
kernelBuilder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

var kernel = kernelBuilder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

kernel.Plugins.AddFromType<CalendarPlugin>("calendar", app.Services);
kernel.Plugins.AddFromType<MailPlugin>("mail", app.Services);
kernel.Plugins.AddFromType<OneDrivePlugin>("oneDrive", app.Services);

OpenAIPromptExecutionSettings openAiPromptExecutionSettings = new() 
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var history = new ChatHistory();

app.Map("/ws/prompt", async (HttpContext context) =>
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            return Results.BadRequest();
        }
        
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        
        var buffer = new byte[4096];
        
        while (true)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                break;
            }

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            history.AddUserMessage(message);
        
            var stream = chatCompletionService.GetStreamingChatMessageContentsAsync(
                history,
                executionSettings: openAiPromptExecutionSettings,
                kernel: kernel);

            AuthorRole? role = null;
        
            await foreach (var chunk in stream)
            {
                if (chunk.Role != null)
                {
                    role = chunk.Role;
                }

                if (role == null)
                {
                    throw new Exception();
                }

                var content = chunk.Content ?? string.Empty;
                history.AddMessage((AuthorRole)role, content);

                await SendChunkAsync(content, webSocket);
            }
            
            await SendChunkAsync("[DONE]", webSocket);
        }
        
        return Results.Ok();
    })
    .WithName("Prompt");

app.Run();
return;

Task SendChunkAsync(string message, WebSocket webSocket)
{
    var bytes = Encoding.UTF8.GetBytes(message);
                
    return webSocket.SendAsync(
        new ArraySegment<byte>(bytes),
        WebSocketMessageType.Text,
        endOfMessage: true,
        cancellationToken: CancellationToken.None
    );
}