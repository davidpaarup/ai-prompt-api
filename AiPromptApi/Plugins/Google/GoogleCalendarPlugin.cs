using System.ComponentModel;
using AiPromptApi.Model;
using AiPromptApi.Services;
using Microsoft.SemanticKernel;

namespace AiPromptApi.Plugins.Google;

public class GoogleCalendarPlugin(GoogleClientFactory clientFactory)
{
    [KernelFunction("fetch_next_month_events_from_google")]
    [Description("Fetches events from the Google calendar for the current month. The times are in UTC.")]
    private async Task<IEnumerable<DomainEvent>> FetchCurrentMonthCalendarEventsAsync()
    {
        var currentDate = DateTime.Now;
        var startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        
        var startOffset = new DateTimeOffset(startOfMonth);
        var endOffset = new DateTimeOffset(endOfMonth);
            
        IEnumerable<DomainEvent> events = [];
        string? pageToken = null;
        var calendarService = await clientFactory.CreateCalendarServiceAsync();

        while (true)
        {
            var request = calendarService.Events.List("primary");
            request.TimeMinDateTimeOffset = startOffset;
            request.TimeMaxDateTimeOffset = endOffset;
            request.TimeZone = "UTC";
            request.PageToken = pageToken;
            var googleEvents = await request.ExecuteAsync();
            const string format = "yyyy-MM-dd HH:mm";

            foreach (var _event in googleEvents.Items)
            {
                var eventStart = _event.Start?.DateTimeDateTimeOffset;
                var eventEnd = _event.End?.DateTimeDateTimeOffset;
                
                if (eventStart == null || eventEnd == null)
                {
                    throw new Exception();
                }
                
                var castedStart = (DateTimeOffset)eventStart;
                var castedEnd = (DateTimeOffset)eventEnd;
                
                var start = castedStart.ToString(format);
                var end = castedEnd.ToString(format);
                var domainEvent = new DomainEvent(start, end, _event.Summary);
                events = events.Append(domainEvent);
            }
            
            pageToken = googleEvents.NextPageToken;

            if (string.IsNullOrWhiteSpace(pageToken))
            {
                break;
            }
        }

        return events;
    }
}