namespace AiPromptApi;

public interface IAccountRepository
{
    Task<string> GetRefreshTokenAsync(string userId, string providerId);
}