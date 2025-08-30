using Grafana.OpenTelemetry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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

const string serviceName = "semantic-kernel-api";

builder.Logging.AddOpenTelemetry(options =>
{
    options
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName))
        .AddConsoleExporter()
        .AddOtlpExporter()
        .UseGrafana();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter()
        .UseGrafana())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter()
        .UseGrafana());

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

app.MapPost("/prompt", async (HttpContext context, [FromBody] PromptInput payload) =>
{
    history.AddUserMessage(payload.Message);

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
        
        await context.Response.WriteAsync("a" + content);
        await context.Response.Body.FlushAsync();
    }
})
.WithName("Prompt");

app.Run();

internal class PromptInput(string message)
{
    public string Message { get; } = message;
}