using System.ComponentModel;
using AiPromptApi.Model;
using AiPromptApi.Services;
using Microsoft.SemanticKernel;

namespace AiPromptApi.Plugins.Microsoft;

public class MicrosoftCalendarPlugin(GraphClientFactory graphClientFactory)
{
    [KernelFunction("fetch_next_month_events")]
    [Description("Fetches events from the Outlook calendar for the current month. The times are in UTC.")]
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
                const string format = "yyyy-MM-ddTHH:mm:ss.fffK";
                
                var formattedStart = startOfMonth.ToString(format);
                var formattedEnd = endOfMonth.ToString(format);
                
                config.QueryParameters.Filter =
                    $"start/dateTime ge '{formattedStart}' and end/dateTime le " +
                    $"'{formattedEnd}'";
                
                config.QueryParameters.Orderby = ["start/dateTime"];
            });

        if (events?.Value == null)
        {
            throw new Exception();
        }

        foreach (var calendarEvent in events.Value)
        {
            var start = calendarEvent.Start?.DateTime;
            var end = calendarEvent.End?.DateTime;
            
            if (start == null || end == null)
            {
                throw new Exception();
            }
            
            const string format = "yyyy-MM-dd HH:mm";
            
            var startTime = DateTime.Parse(start).ToString(format);
            var endTime = DateTime.Parse(end).ToString(format);
            
            var subject = calendarEvent.Subject ?? "";
            
            var e = new DomainEvent(startTime, endTime, subject);
            calendarEvents = calendarEvents.Append(e);
        }

        return calendarEvents;
    }
}