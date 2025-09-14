using System.ComponentModel;
using AiPromptApi.Services;
using Google.Apis.Gmail.v1.Data;
using Microsoft.SemanticKernel;

namespace AiPromptApi.Plugins.Google;

public class GoogleMailPlugin(GoogleClientFactory clientFactory)
{
    [KernelFunction("send_email")]
    [Description("Sends an email via Gmail with the specified subject and body to the given recipient.")]
    private async Task<bool> SendEmailAsync(string subject, string body, string recipient)
    {
        var emailService = await clientFactory.CreateEmailServiceAsync();
        
        var email = new Message
        {
            Raw = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                $"To: {recipient}\r\n" +
                $"Subject: {subject}\r\n" +
                "\r\n" +
                $"{body}"))
        };
        
        await emailService.Users.Messages.Send(email, "me").ExecuteAsync();
        return true;
    }
}