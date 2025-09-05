using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AiPromptApi;

public class GraphClientFactory(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, 
    IAccountRepository accountRepository)
{
    public async Task<GraphServiceClient> CreateAsync()
    {
        var accessToken = await GetAccessTokenAsync();
        var tokenProvider = new AccessTokenProvider(accessToken);
        var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
        return new GraphServiceClient(authProvider);
    }
    
    private class AccessTokenProvider(string accessToken) : IAccessTokenProvider
    {
        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, 
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(accessToken);
        }

        public AllowedHostsValidator AllowedHostsValidator { get; } = new();
    }
    
    private async Task<string> GetAccessTokenAsync()
    {
        var clientId = configuration["ClientId"];
        var clientSecret = configuration["ClientSecret"];
        var tenantId = configuration["TenantId"];
        
        if (httpContextAccessor.HttpContext == null || clientId == null || clientSecret == null || tenantId == null)
        {
            throw new Exception();
        }
        
        var userId = httpContextAccessor.HttpContext.User.Claims.Single(c => c.Type == "id").Value;
        var refreshToken = await accountRepository.GetRefreshTokenAsync(userId, "microsoft");
        
        var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        IEnumerable<string> scopes =
        [
            "User.Read",
            "Mail.Read",
            "Mail.Send",
            "Files.Read",
            "Files.Read.All",
            "Calendars.Read"
        ];
        
        var parameters = new Dictionary<string, string>
        {
            {"grant_type", "refresh_token"},
            {"refresh_token", refreshToken},
            {"client_id", clientId},
            {"client_secret", clientSecret},
            {"scope", string.Join(" ", scopes)}
        };

        var content = new FormUrlEncodedContent(parameters);

        using var client = new HttpClient();
        var response = await client.PostAsync(tokenEndpoint, content);
        
        var jsonResponse = await response.Content.ReadAsStringAsync();

        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);

        if (tokenResponse == null)
        {
            throw new Exception();
        }
        
        return tokenResponse.AccessToken;
    }
    
    public class TokenResponse(string accessToken)
    {
        [JsonPropertyName("access_token")] 
        public string AccessToken { get; set; } = accessToken;
    }
}