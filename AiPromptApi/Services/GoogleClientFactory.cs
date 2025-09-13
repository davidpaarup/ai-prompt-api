using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;

namespace AiPromptApi.Services;

public class GoogleClientFactory(IUserService userService,
    IAccountRepository accountRepository)
{
    public async Task<CalendarService> CreateCalendarServiceAsync()
    {
        var userId = userService.GetUserId();
        var accessToken = await accountRepository.GetAccessTokenAsync(userId, "google");
        
        var credential = GoogleCredential.FromAccessToken(accessToken);

        var initializer = new BaseClientService.Initializer
        {
            HttpClientInitializer = credential
        };

        return new CalendarService(initializer);
    }
}