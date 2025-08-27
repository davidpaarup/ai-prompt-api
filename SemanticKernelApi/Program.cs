using Microsoft.AspNetCore.Mvc;
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

app.MapPost("/prompt", async ([FromBody] PromptInput input) =>
    {
        history.AddUserMessage(input.Prompt);
        
        var result = await chatCompletionService.GetChatMessageContentAsync(
            history,
            executionSettings: openAiPromptExecutionSettings,
            kernel: kernel);
        
        history.AddMessage(result.Role, result.Content ?? string.Empty);
        
        return Results.Ok(result.Content);
    })
    .WithName("Prompt");

app.Run();

internal class PromptInput(string prompt)
{
    public string Prompt { get; } = prompt;
}