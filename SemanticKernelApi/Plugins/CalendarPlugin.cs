using System.ComponentModel;
using Microsoft.SemanticKernel;
using SemanticKernelApi.Model;

namespace SemanticKernelApi.Plugins;

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