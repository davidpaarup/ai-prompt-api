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
    
    public GraphClient(IEnumerable<string> scopes, IConfiguration config, IHttpContextAccessor httpContextAccessor)
    {   
        var cred = new ClientSecretCredential(
            config["tenantId"], config["ClientId"], config["ClientSecret"]);
        
        _client = new GraphServiceClient(cred, scopes);
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetAccessToken()
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        var accessToken = authHeader?.Replace("Bearer ", "");

        if (accessToken == null)
        {
            throw new UnauthorizedAccessException();
        }

        return accessToken;
    }

    public async Task<string> GetFileContentAsync(string fileId)
    {
        var split = fileId.Split('!');
        var driveId = split[0];
        
        var content = await _client.Drives[driveId].Items[fileId].Content.GetAsync(config =>
        {
            var accessToken = GetAccessToken();
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
        var drive = await _client.Me
            .Drive.GetAsync(ItemsConfiguration);
        
        if (drive == null)
        {
            throw new Exception();
        }
        
        var items = await _client.Drives[drive.Id]
            .Items["root"].Children.GetAsync(ItemConfiguration);

        if (items?.Value == null)
        {
            throw new Exception();
        }
        
        var result = items.Value.Select(v =>
        {
            if (v.Id == null || v.Name == null)
            {
                throw new Exception();
            }
            
            return new DomainFile(v.Id, v.Name);
        });

        return result;
        
        void ItemsConfiguration(RequestConfiguration<Microsoft.Graph.Me.Drive.DriveRequestBuilder
                .DriveRequestBuilderGetQueryParameters> 
            
            requestConfiguration)
        {
            var accessToken = GetAccessToken();
            requestConfiguration.Headers.Add("Authorization", accessToken);
        }
        
        void ItemConfiguration(RequestConfiguration<Microsoft.Graph.Drives.Item.Items.Item.Children
                .ChildrenRequestBuilder.ChildrenRequestBuilderGetQueryParameters>
            
            requestConfiguration)
        {
            var accessToken = GetAccessToken();
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
            var accessToken = GetAccessToken();
            requestConfiguration.Headers.Add("Authorization", accessToken);
        }
    }

    public async Task<IEnumerable<DomainMessage>> FetchEmailsFromInboxAsync()
    {
        var messagePage = await _client.Me
            .MailFolders["Inbox"]
            .Messages
            .GetAsync(config =>
            {
                config.QueryParameters.Select = ["from", "isRead", "receivedDateTime", "subject"];
                config.QueryParameters.Top = 25;
                config.QueryParameters.Orderby = ["receivedDateTime DESC"];
                var accessToken = GetAccessToken();
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
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"start/dateTime ge '{startOfMonth:yyyy-MM-ddTHH:mm:ss.fffK}' and end/dateTime le '{endOfMonth:yyyy-MM-ddTHH:mm:ss.fffK}'";
                    requestConfiguration.QueryParameters.Orderby = ["start/dateTime"];
                    var accessToken = GetAccessToken();
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