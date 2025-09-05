using Grafana.OpenTelemetry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using AiPromptApi;
using AiPromptApi.Plugins;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

async Task<IEnumerable<SecurityKey>> GetSigningKeysFromJwks(string? kid, string issuer)
{
    var jwksUrl = $"{issuer}/api/auth/jwks";
    
    using var httpClient = new HttpClient();
    var response = await httpClient.GetStringAsync(jwksUrl);
    
    var jwks = new JsonWebKeySet(response);

    if (string.IsNullOrEmpty(kid))
    {
        return jwks.Keys;
    }
        
    var key = jwks.Keys.FirstOrDefault(k => k.Kid == kid);

    if (key == null)
    {
        return jwks.Keys;
    }
            
    return [key];
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<GraphClientFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAccountRepository, AccountRepository>();

var issuer = builder.Configuration["Issuer"];

if (issuer == null)
{
    throw new Exception();
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
            
            IssuerSigningKeyResolver = (_, _, kid, _) => 
                GetSigningKeysFromJwks(kid, issuer).GetAwaiter().GetResult()
        };
        
    });

builder.Services.AddAuthorization();

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
kernel.Plugins.AddFromType<TextToAudioPlugin>("textToAudio", app.Services);

OpenAIPromptExecutionSettings openAiPromptExecutionSettings = new() 
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var history = new ChatHistory();

app.MapGet("/", () => "Semantic Kernel API is running.");

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
        
        await context.Response.WriteAsync(content);
        await context.Response.Body.FlushAsync();
    }
})
.RequireAuthorization()
.WithName("Prompt");

app.Run();

internal class PromptInput(string message)
{
    public string Message { get; } = message;
}