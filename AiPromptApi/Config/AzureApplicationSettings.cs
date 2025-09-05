namespace AiPromptApi.Config;

public class AzureApplicationSettings(string clientId, string clientSecret, string tenantId)
{
    public string ClientId { get; set; } = clientId;
    public string ClientSecret { get; set; } = clientSecret;
    public string TenantId { get; set; } = tenantId;
}