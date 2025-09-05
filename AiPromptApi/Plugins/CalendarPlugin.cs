using System.ComponentModel;
using AiPromptApi.Model;
using AiPromptApi.Services;
using Microsoft.SemanticKernel;

namespace AiPromptApi.Plugins;

public class CalendarPlugin(GraphClientFactory graphClientFactory)
{
    [KernelFunction("fetch_next_month_events")]
    [Description("Fetches calendar events for the current month.")]
    private async Task<IEnumerable<DomainEvent>> FetchCurrentMonthCalendarEventsAsync()
    {
        var currentDate = DateTime.Now;
        var startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        IEnumerable<DomainEvent> calendarEvents = [];

        var client = await graphClientFactory.CreateAsync();
        
        var events = await client.Me.Calendar.Events
            .GetAsync(config =>
            {
                config.QueryParameters.Filter =
                    $"start/dateTime ge '{startOfMonth:yyyy-MM-ddTHH:mm:ss.fffK}' and end/dateTime le '{endOfMonth:yyyy-MM-ddTHH:mm:ss.fffK}'";
                config.QueryParameters.Orderby = ["start/dateTime"];
            });

        if (events?.Value == null)
        {
            throw new Exception();
        }

        foreach (var calendarEvent in events.Value)
        {
            var startTime = DateTime.Parse(calendarEvent.Start?.DateTime ?? "").ToString("yyyy-MM-dd HH:mm");
            var endTime = DateTime.Parse(calendarEvent.End?.DateTime ?? "").ToString("HH:mm");
            var e = new DomainEvent(startTime, endTime, calendarEvent.Subject ?? "");
            calendarEvents = calendarEvents.Append(e);
        }

        return calendarEvents;
    }
}