namespace AiPromptApi.Services;

public interface IAccountRepository
{
    Task<string> GetAccessTokenAsync(string userId, string providerId);
}