using System.ComponentModel;
using AiPromptApi.Model;
using Microsoft.SemanticKernel;

namespace AiPromptApi.Plugins;

public class CalendarPlugin
{
    private readonly GraphClient _graphClient;

    public CalendarPlugin(GraphClientFactory graphClientFactory)
    {
        IEnumerable<string> scopes = ["Calendars.Read"];
        _graphClient = graphClientFactory.Create(scopes);
    }
    
    [KernelFunction("fetch_next_month_events")]
    [Description("Fetches calendar events for the current month.")]
    private Task<IEnumerable<DomainEvent>> FetchCurrentMonthCalendarEvents()
    {
        return _graphClient.FetchCurrentMonthCalendarEventsAsync();
    }
}