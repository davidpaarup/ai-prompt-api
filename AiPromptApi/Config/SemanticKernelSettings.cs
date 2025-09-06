namespace AiPromptApi.Config;

public class SemanticKernelSettings(string modelId)
{
    public string ModelId { get; set; } = modelId;
}