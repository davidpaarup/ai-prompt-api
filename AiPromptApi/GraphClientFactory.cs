namespace AiPromptApi;

public class GraphClientFactory(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, 
    IAccountRepository accountRepository)
{
    public GraphClient Create(IEnumerable<string> scopes)
    {
        return new GraphClient(scopes, configuration, httpContextAccessor, accountRepository);
    }
}