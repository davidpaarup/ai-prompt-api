namespace AiPromptApi.Services;

public class UserService(IHttpContextAccessor httpContextAccessor) : IUserService
{
    public string GetUserId()
    {
        if (httpContextAccessor.HttpContext == null )
        {
            throw new Exception();
        }
        
        return httpContextAccessor.HttpContext.User.Claims.Single(c => c.Type == "id").Value;
    }
}