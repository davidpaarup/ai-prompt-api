namespace AiPromptApi.Services;

public interface IApiTokenRepository
{
    Task<string> GetAsync(string userId);
}