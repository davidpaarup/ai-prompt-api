using System.ComponentModel;
using AiPromptApi.Model;
using Microsoft.SemanticKernel;

namespace AiPromptApi.Plugins;

public class MailPlugin
{
    private readonly GraphClient _graphClient;

    public MailPlugin(GraphClientFactory graphClientFactory)
    {
        IEnumerable<string> scopes = ["mail.read", "mail.send" ];
        _graphClient = graphClientFactory.Create(scopes);
    }

    [KernelFunction("send_email")]
    [Description("Sends an email with the specified subject and body to the given recipient.")]
    private Task<bool> SendEmailAsync(string subject, string body, string recipient)
    {
        return _graphClient.SendEmailAsync(subject, body, recipient);
    }
    
    [KernelFunction("fetch_mails_from_inbox")]
    [Description("Fetches all the emails from the inbox.")]
    private Task<IEnumerable<DomainMessage>> FetchCurrentMonthCalendarEvents()
    {
        return _graphClient.FetchEmailsFromInboxAsync();
    }
}