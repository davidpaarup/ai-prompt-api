namespace AiPromptApi.Services;

public interface IKernelService
{
    IAsyncEnumerable<string> GetReplyAsync(string input);
}