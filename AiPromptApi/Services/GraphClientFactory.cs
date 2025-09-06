using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AiPromptApi.Services;

public class GraphClientFactory(IUserService userService,
    IAccountRepository accountRepository)
{
    public async Task<GraphServiceClient> CreateAsync()
    {
        var userId = userService.GetUserId();
        var accessToken = await accountRepository.GetAccessTokenAsync(userId, "microsoft");
        var tokenProvider = new AccessTokenProvider(accessToken);
        var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
        return new GraphServiceClient(authProvider);
    }
    
    private class AccessTokenProvider(string accessToken) : IAccessTokenProvider
    {
        public Task<string> GetAuthorizationTokenAsync(Uri uri, 
            Dictionary<string, object>? additionalAuthenticationContext = null, 
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(accessToken);
        }

        public AllowedHostsValidator AllowedHostsValidator { get; } = new();
    }
}