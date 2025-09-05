namespace AiPromptApi.Config;

public class SemanticKernelSettings(string modelId, string openAiKey)
{
    public string ModelId { get; set; } = modelId;
    public string OpenAiKey { get; set; } = openAiKey;
}