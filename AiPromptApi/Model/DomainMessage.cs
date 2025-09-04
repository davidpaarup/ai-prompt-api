namespace AiPromptApi.Model;

public class DomainMessage(string title, string body)
{
    public string Title { get; } = title;
    public string Body { get; } = body;
}