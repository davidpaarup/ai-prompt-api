namespace SemanticKernelApi;

public class GraphClientFactory(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
{
    public GraphClient Create(IEnumerable<string> scopes)
    {
        return new GraphClient(scopes, configuration, httpContextAccessor);
    }
}