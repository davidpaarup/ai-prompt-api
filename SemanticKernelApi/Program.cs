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
        policy.WithOrigins("http://localhost:5173", "https://ai-prompt-drab-eta.vercel.app")
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

app.MapGet("/auth-callback", async (HttpContext context, string code) =>
    {
        var tenantId = app.Configuration["TenantId"];
        var clientId = app.Configuration["ClientId"];
        var clientSecret = app.Configuration["ClientSecret"];
        
        if (clientId == null || clientSecret == null)
        {
            throw new Exception();
        }

        IEnumerable<string> scope =
        [
            "files.read",
            "files.read.all",
            "mail.read",
            "mail.send",
            "Calendars.Read"
        ];
        
        var scopeString = string.Join(" ", scope);
        
        using var httpClient = new HttpClient();

        var request = context.Request;
        var currentUrl = $"{request.Scheme}://{request.Host}{request.Path}";

        var contentElements = new List<KeyValuePair<string?, string?>>
        {
            new("client_id", clientId),
            new("scope", scopeString),
            new("code", code),
            new("redirect_uri", currentUrl),
            new("grant_type", "authorization_code"),
            new("client_secret", clientSecret)
        };
            
        var content = new FormUrlEncodedContent(contentElements);
        
        var result = await httpClient.PostAsync(
            $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token", 
            content);

        if (!result.IsSuccessStatusCode)
        {
            var error = await result.Content.ReadAsStringAsync();
        }
        
        var responseString = await result.Content.ReadFromJsonAsync<AuthResult>();

        if (responseString == null)
        {
            throw new Exception();
        }

        return responseString.access_token;
    })
    .WithName("AuthCallback");

app.Run();

internal class PromptInput(string prompt)
{
    public string Prompt { get; } = prompt;
}

internal class AuthResult {
    public string access_token { get; set; }
}