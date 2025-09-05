namespace AiPromptApi.Services;

public interface IAccountRepository
{
    Task<string> GetRefreshTokenAsync(string userId, string providerId);
}