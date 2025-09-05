using System.ComponentModel;
using AiPromptApi.Model;
using AiPromptApi.Services;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel;

namespace AiPromptApi.Plugins;

public class MailPlugin(GraphClientFactory graphClientFactory)
{
    [KernelFunction("send_email")]
    [Description("Sends an email with the specified subject and body to the given recipient.")]
    private async Task<bool> SendEmailAsync(string subject, string body, string recipient)
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

        var client = await graphClientFactory.CreateAsync();
        
        await client.Me
            .SendMail
            .PostAsync(new SendMailPostRequestBody
            {
                Message = message
            });

        return true;
    }
    
    [KernelFunction("fetch_mails_from_inbox")]
    [Description("Fetches all the emails from the inbox.")]
    private async Task<IEnumerable<DomainMessage>> FetchCurrentMonthCalendarEventsAsync()
    {
        var client = await graphClientFactory.CreateAsync();
        
        var messagePage = await client.Me
            .MailFolders["Inbox"]
            .Messages
            .GetAsync(config =>
            {
                config.QueryParameters.Select = ["from", "isRead", "receivedDateTime", "subject"];
                config.QueryParameters.Top = 25;
                config.QueryParameters.Orderby = ["receivedDateTime DESC"];
            });

        if (messagePage?.Value == null)
        {
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
}