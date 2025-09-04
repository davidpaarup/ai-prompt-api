namespace AiPromptApi.Model;

public class DomainEvent(string start, string end, string subject)
{
    public string Start { get; } = start;
    public string End { get; } = end;
    public string Subject { get; } = subject;
}