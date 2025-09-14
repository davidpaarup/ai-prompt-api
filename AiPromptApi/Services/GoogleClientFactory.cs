using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;

namespace AiPromptApi.Services;

public class GoogleClientFactory(IUserService userService,
    IAccountRepository accountRepository)
{
    private async Task<BaseClientService.Initializer> GetInitializerAsync()
    {
        var userId = userService.GetUserId();
        var accessToken = await accountRepository.GetAccessTokenAsync(userId, "google");
        
        var credential = GoogleCredential.FromAccessToken(accessToken);

        var initializer = new BaseClientService.Initializer
        {
            HttpClientInitializer = credential
        };

        return initializer;
    }
    
    public async Task<CalendarService> CreateCalendarServiceAsync()
    {
        var initializer = await GetInitializerAsync();
        return new CalendarService(initializer);
    }
    
    public async Task<GmailService> CreateEmailServiceAsync()
    {
        var initializer = await GetInitializerAsync();
        return new GmailService(initializer);
    }
}