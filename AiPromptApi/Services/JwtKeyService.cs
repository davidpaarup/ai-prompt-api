using Microsoft.IdentityModel.Tokens;

namespace AiPromptApi.Services;

public static class JwtKeyService
{
    public static async Task<IEnumerable<SecurityKey>> GetSigningKeysFromJwks(string? kid, string issuer)
    {
        var jwksUrl = $"{issuer}/api/auth/jwks";
        
        var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync(jwksUrl);
        
        var jwks = new JsonWebKeySet(response);

        if (string.IsNullOrEmpty(kid))
        {
            return jwks.Keys;
        }
            
        var key = jwks.Keys.FirstOrDefault(k => k.Kid == kid);

        if (key == null)
        {
            return jwks.Keys;
        }
                
        return [key];
    }
}