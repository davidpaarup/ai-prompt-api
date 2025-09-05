using AiPromptApi.Plugins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AiPromptApi.Controllers;

[Route("/")]
[ApiController]
public class BaseController(IConfiguration configuration, IServiceProvider serviceProvider) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Semantic Kernel API is running.");
    }
    
    [HttpPost("prompt")]
    [Authorize]
    public async Task<IActionResult> Post([FromBody] PromptInput payload)
    {
        var modelId = configuration["ModelId"];
        var apiKey = configuration["OpenAIKey"];

        if (modelId == null || apiKey == null)
        {
            throw new Exception("OpenAI configuration missing");
        }

        var kernelBuilder = Kernel.CreateBuilder().AddOpenAIChatCompletion(modelId, apiKey);

        var kernel = kernelBuilder.Build();
        kernel.Plugins.AddFromType<CalendarPlugin>("calendar", serviceProvider);
        kernel.Plugins.AddFromType<MailPlugin>("mail", serviceProvider);
        kernel.Plugins.AddFromType<OneDrivePlugin>("oneDrive", serviceProvider);
        kernel.Plugins.AddFromType<TextToAudioPlugin>("textToAudio", serviceProvider);
        
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        
        history.AddUserMessage(payload.Message);
        
        OpenAIPromptExecutionSettings executionSettings = new() 
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var stream = chatCompletionService.GetStreamingChatMessageContentsAsync(
            history,
            executionSettings,
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

            await HttpContext.Response.WriteAsync(content);
            await HttpContext.Response.Body.FlushAsync();
        }

        return new EmptyResult();
    }
}

public class PromptInput
{
    public string Message { get; set; } = string.Empty;
}