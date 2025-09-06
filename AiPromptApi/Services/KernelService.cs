using AiPromptApi.Config;
using AiPromptApi.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AiPromptApi.Services;

public class KernelService(SemanticKernelSettings semanticKernelSettings, IServiceProvider serviceProvider,
    IUserService userService, IApiTokenRepository apiTokenRepository) : IKernelService
{
    public async IAsyncEnumerable<string> GetReplyAsync(string input)
    {
        var modelId = semanticKernelSettings.ModelId;
        var userId = userService.GetUserId();
        var apiKey = await apiTokenRepository.GetAsync(userId);

        var kernelBuilder = Kernel.CreateBuilder().AddOpenAIChatCompletion(modelId, apiKey);

        var kernel = kernelBuilder.Build();
        kernel.Plugins.AddFromType<CalendarPlugin>(nameof(CalendarPlugin), serviceProvider);
        kernel.Plugins.AddFromType<MailPlugin>(nameof(MailPlugin), serviceProvider);
        kernel.Plugins.AddFromType<OneDrivePlugin>(nameof(OneDrivePlugin), serviceProvider);
        kernel.Plugins.AddFromType<TextToAudioPlugin>(nameof(TextToAudioPlugin), serviceProvider);
        
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        
        history.AddUserMessage(input);
        
        OpenAIPromptExecutionSettings executionSettings = new() 
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var iterator = chatCompletionService.GetStreamingChatMessageContentsAsync(
            history,
            executionSettings,
            kernel);
        
        await foreach (var chunk in iterator)
        {
            var content = chunk.Content ?? string.Empty;
            yield return content;
        } 
    }
}