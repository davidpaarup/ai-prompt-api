using System.Text.Json;
using System.Text.Json.Serialization;
using AiPromptApi.Model;
using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;

namespace AiPromptApi;

public class GraphClient
{
    private readonly GraphServiceClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAccountRepository _accountRepository;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly IEnumerable<string> _scopes;

    public GraphClient(IEnumerable<string> scopes, IConfiguration config, IHttpContextAccessor httpContextAccessor,
        IAccountRepository accountRepository)
    {
        var clientId = config["clientId"];
        var clientSecret = config["clientSecret"];
        var tenantId = config["tenantId"];

        if (clientId == null || clientSecret == null || tenantId == null)
        {
            throw new Exception();
        }

        _clientId = clientId;
        _clientSecret = clientSecret;
        
        var enumerable = scopes as string[] ?? scopes.ToArray();
        _scopes = enumerable;

        var cred = new ClientSecretCredential(
            tenantId, _clientId, _clientSecret);
        
        _client = new GraphServiceClient(cred, enumerable);
        _httpContextAccessor = httpContextAccessor;
        _accountRepository = accountRepository;
    }
    
    private async Task<string> GetAccessTokenAsync()
    {
        Console.WriteLine("Starting GetAccessTokenAsync");
        
        if (_httpContextAccessor.HttpContext == null)
        {
            Console.WriteLine("HttpContext is null");
            throw new Exception();
        }
        
        var userId = _httpContextAccessor.HttpContext.User.Claims.Single(c => c.Type == "id").Value;
        Console.WriteLine($"Retrieved userId: {userId}");
        
        var refreshToken = await _accountRepository.GetRefreshTokenAsync(userId, "microsoft");
        Console.WriteLine($"Retrieved refresh token: {refreshToken?.Substring(0, Math.Min(20, refreshToken.Length))}...");
        
        const string tokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
        Console.WriteLine($"Using token endpoint: {tokenEndpoint}");

        var parameters = new Dictionary<string, string>
        {
            {"grant_type", "refresh_token"},
            {"refresh_token", refreshToken},
            {"client_id", _clientId},
            {"client_secret", _clientSecret},
            {"scope", string.Join(" ", _scopes)}
        };

        Console.WriteLine($"Token request parameters: grant_type=refresh_token, client_id={_clientId}, scopes={string.Join(" ", _scopes)}");

        var content = new FormUrlEncodedContent(parameters);

        using var client = new HttpClient();
        Console.WriteLine("Making POST request to token endpoint");
        var response = await client.PostAsync(tokenEndpoint, content);
        Console.WriteLine($"Token response status: {response.StatusCode}");
        
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Token response content: {jsonResponse}");

        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);

        if (tokenResponse == null)
        {
            Console.WriteLine("Failed to deserialize token response");
            throw new Exception();
        }
        
        Console.WriteLine($"Successfully retrieved access token: {tokenResponse.AccessToken?.Substring(0, Math.Min(20, tokenResponse.AccessToken.Length))}...");
        return tokenResponse.AccessToken;
    }

    public class TokenResponse(string accessToken)
    {
        [JsonPropertyName("access_token")] 
        public string AccessToken { get; set; } = accessToken;
    }

    public async Task<string> GetFileContentAsync(string fileId)
    {
        var split = fileId.Split('!');
        var driveId = split[0];
        
        var content = await _client.Drives[driveId].Items[fileId].Content.GetAsync(async void (config) =>
        {
            var accessToken = await GetAccessTokenAsync();
            config.Headers.Add("Authorization", accessToken);
        });
    
        if (content == null)
        {
            throw new Exception();
        }
    
        using var reader = new StreamReader(content);
        return await reader.ReadToEndAsync();
    
    }

    public async Task<IEnumerable<DomainFile>> GetOneDriveItemsAsync()
    {
        Console.WriteLine("Starting GetOneDriveItemsAsync");
        
        var drive = await _client.Me
            .Drive.GetAsync(ItemsConfiguration);
        
        if (drive == null)
        {
            Console.WriteLine("Drive is null");
            throw new Exception();
        }
        
        Console.WriteLine($"Retrieved drive with ID: {drive.Id}");
        
        var items = await _client.Drives[drive.Id]
            .Items["root"].Children.GetAsync(ItemConfiguration);

        if (items?.Value == null)
        {
            Console.WriteLine("Items or Items.Value is null");
            throw new Exception();
        }
        
        Console.WriteLine($"Retrieved {items.Value.Count} items from OneDrive");
        
        var result = items.Value.Select(v =>
        {
            if (v.Id == null || v.Name == null)
            {
                Console.WriteLine($"Item with null ID or Name found");
                throw new Exception();
            }
            
            Console.WriteLine($"Processing item: {v.Name} (ID: {v.Id})");
            return new DomainFile(v.Id, v.Name);
        });

        Console.WriteLine("GetOneDriveItemsAsync completed successfully");
        return result;
        
        void ItemsConfiguration(RequestConfiguration<Microsoft.Graph.Me.Drive.DriveRequestBuilder
                .DriveRequestBuilderGetQueryParameters> 
            
            requestConfiguration)
        {
            var accessToken = GetAccessTokenAsync().GetAwaiter().GetResult();
            requestConfiguration.Headers.Add("Authorization", accessToken);
        }
        
        void ItemConfiguration(RequestConfiguration<Microsoft.Graph.Drives.Item.Items.Item.Children
                .ChildrenRequestBuilder.ChildrenRequestBuilderGetQueryParameters>
            
            requestConfiguration)
        {
            var accessToken = GetAccessTokenAsync().GetAwaiter().GetResult();
            requestConfiguration.Headers.Add("Authorization", accessToken);
        }
    }

    public async Task<bool> SendEmailAsync(string subject, string body, string recipient)
    {
        var message = new Message
        {
            Subject = subject,
            Body = new ItemBody
            {
                Content = body,
                ContentType = BodyType.Text
            },
            ToRecipients =
            [
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = recipient
                    }
                }
            ]
        };

        await _client.Me
            .SendMail
            .PostAsync(new SendMailPostRequestBody
            {
                Message = message
            }, Configuration);

        return true;
        
        void Configuration(RequestConfiguration<DefaultQueryParameters>
            requestConfiguration)
        {
            var accessToken = GetAccessTokenAsync().GetAwaiter().GetResult();
            requestConfiguration.Headers.Add("Authorization", accessToken);
        }
    }

    public async Task<IEnumerable<DomainMessage>> FetchEmailsFromInboxAsync()
    {
        var messagePage = await _client.Me
            .MailFolders["Inbox"]
            .Messages
            .GetAsync(async void (config) =>
            {
                config.QueryParameters.Select = ["from", "isRead", "receivedDateTime", "subject"];
                config.QueryParameters.Top = 25;
                config.QueryParameters.Orderby = ["receivedDateTime DESC"];
                var accessToken = await GetAccessTokenAsync();
                config.Headers.Add("Authorization", accessToken);
            });

        if (messagePage?.Value == null)
        {
            Console.WriteLine("No results returned.");
            return [];
        }

        List<DomainMessage> messages = [];

        foreach (var message in messagePage.Value)
        {
            var subject = message.Subject ?? "";
            var body = message.Body?.Content ?? "";
            var m = new DomainMessage(subject, body);
            messages.Add(m);
        }

        return messages;
    }

    public async Task<IEnumerable<DomainEvent>> FetchCurrentMonthCalendarEventsAsync()
    {
        var currentDate = DateTime.Now;
        var startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        IEnumerable<DomainEvent> calendarEvents = [];
        
        try
        {
            var events = await _client.Me.Calendar.Events
                .GetAsync(async void (requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Filter = $"start/dateTime ge '{startOfMonth:yyyy-MM-ddTHH:mm:ss.fffK}' and end/dateTime le '{endOfMonth:yyyy-MM-ddTHH:mm:ss.fffK}'";
                    requestConfiguration.QueryParameters.Orderby = ["start/dateTime"];
                    var accessToken = await GetAccessTokenAsync();
                    requestConfiguration.Headers.Add("Authorization", accessToken);
                });

            if (events?.Value?.Count > 0)
            {
                foreach (var calendarEvent in events.Value)
                {
                    var startTime = DateTime.Parse(calendarEvent.Start?.DateTime ?? "").ToString("yyyy-MM-dd HH:mm");
                    var endTime = DateTime.Parse(calendarEvent.End?.DateTime ?? "").ToString("HH:mm");
                    var e = new DomainEvent(startTime, endTime, calendarEvent.Subject ?? "");
                    calendarEvents = calendarEvents.Append(e);
                    return calendarEvents;
                }
            }
            else
            {
                Console.WriteLine("No events found for this month.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching calendar events: {ex.Message}");
            
            if (ex.Message.Contains("Forbidden") || ex.Message.Contains("403"))
            {
                Console.WriteLine("Make sure your application has the necessary permissions (Calendars.Read) and admin consent has been granted.");
            }
        }

        throw new Exception();
    }
}