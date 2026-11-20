using System.Globalization;
using Retailer.Application.Common.Exceptions;
using Retailer.Domain.Legacy;

namespace Retailer.Application.Legacy.RecurringChains;

public static class RecurringScheduleEvaluator
{
    public const string Daily = "Daily";
    public const string Weekly = "Weekly";
    public const string BiWeekly = "BiWeekly";
    public const string Monthly = "Monthly";

    private static readonly string[] ShortDayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

    public static string NormalizeFrequency(string? frequency)
    {
        if (string.IsNullOrWhiteSpace(frequency))
            return Daily;

        var trimmed = frequency.Trim();
        if (trimmed.Equals(Daily, StringComparison.OrdinalIgnoreCase))
            return Daily;
        if (trimmed.Equals(Weekly, StringComparison.OrdinalIgnoreCase))
            return Weekly;
        if (trimmed.Equals(BiWeekly, StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("Bi-Weekly", StringComparison.OrdinalIgnoreCase))
            return BiWeekly;
        if (trimmed.Equals(Monthly, StringComparison.OrdinalIgnoreCase))
            return Monthly;

        throw new BadRequestException($"Unsupported recurrence frequency '{frequency}'. Allowed values: Daily, Weekly, BiWeekly, Monthly.");
    }

    public static List<DayOfWeek> ParseDaysOfWeek(string? csv, DayOfWeek? fallbackDay = null)
    {
        var result = new SortedSet<int>();

        if (!string.IsNullOrWhiteSpace(csv))
        {
            foreach (var token in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num) && num >= 0 && num <= 6)
                {
                    result.Add(num);
                }
                else if (Enum.TryParse<DayOfWeek>(token, true, out var parsedDay))
                {
                    result.Add((int)parsedDay);
                }
            }
        }

        if (result.Count == 0 && fallbackDay.HasValue)
        {
            result.Add((int)fallbackDay.Value);
        }

        return result.Select(d => (DayOfWeek)d).ToList();
    }

    public static string? FormatDaysOfWeek(IEnumerable<DayOfWeek>? days)
    {
        if (days is null)
            return null;

        var distinct = days.Select(d => (int)d).Where(d => d >= 0 && d <= 6).Distinct().OrderBy(d => d).ToList();
        return distinct.Count == 0 ? null : string.Join(",", distinct);
    }

    public static void ValidateAndNormalize(
        string? rawFrequency,
        List<DayOfWeek>? requestDaysOfWeek,
        DayOfWeek? requestDayOfWeek,
        int? requestDayOfMonth,
        int? requestInterval,
        out string normalizedFrequency,
        out string? daysOfWeekCsv,
        out DayOfWeek? legacyDayOfWeek,
        out int? dayOfMonth,
        out int interval)
    {
        normalizedFrequency = NormalizeFrequency(rawFrequency);
        daysOfWeekCsv = null;
        legacyDayOfWeek = null;
        dayOfMonth = null;
        interval = Math.Max(1, requestInterval ?? 1);

        var selectedDays = new List<DayOfWeek>();
        if (requestDaysOfWeek is { Count: > 0 })
        {
            selectedDays = requestDaysOfWeek.Distinct().OrderBy(d => (int)d).ToList();
        }
        else if (requestDayOfWeek.HasValue)
        {
            selectedDays = [requestDayOfWeek.Value];
        }

        switch (normalizedFrequency)
        {
            case Daily:
                // If user selected a subset of days (e.g., Mon-Fri), persist them; if all 7 days selected, store null
                if (selectedDays.Count > 0 && selectedDays.Count < 7)
                {
                    daysOfWeekCsv = FormatDaysOfWeek(selectedDays);
                    legacyDayOfWeek = selectedDays[0];
                    interval = 1;
                }
                else
                {
                    daysOfWeekCsv = null;
                    legacyDayOfWeek = null;
                }

                break;

            case Weekly:
                if (selectedDays.Count == 0)
                    throw new BadRequestException("Please select at least one day of the week for a Weekly schedule.");

                daysOfWeekCsv = FormatDaysOfWeek(selectedDays);
                legacyDayOfWeek = selectedDays[0];
                break;

            case BiWeekly:
                if (selectedDays.Count == 0)
                    throw new BadRequestException("Please select at least one day of the week for a Bi-Weekly schedule.");

                daysOfWeekCsv = FormatDaysOfWeek(selectedDays);
                legacyDayOfWeek = selectedDays[0];
                interval = 2;
                break;

            case Monthly:
                var dom = requestDayOfMonth ?? 1;
                if (dom < 1 || dom > 31)
                    throw new BadRequestException("Day of month must be between 1 and 31.");

                dayOfMonth = dom;
                break;
        }
    }

    public static bool IsDueOn(RecurringChain chain, DateOnly date)
    {
        if (chain.StartDate.HasValue && date < chain.StartDate.Value)
            return false;

        var anchorDate = chain.StartDate ?? DateOnly.FromDateTime(chain.CreatedOn);
        var freq = chain.Frequency ?? Daily;

        if (freq.Equals(Daily, StringComparison.OrdinalIgnoreCase))
        {
            var days = ParseDaysOfWeek(chain.DaysOfWeek);
            if (days.Count > 0 && days.Count < 7)
                return days.Contains(date.DayOfWeek);

            var interval = Math.Max(1, chain.Interval);
            if (interval == 1)
                return true;

            var daysDiff = date.DayNumber - anchorDate.DayNumber;
            return daysDiff >= 0 && daysDiff % interval == 0;
        }

        if (freq.Equals(Weekly, StringComparison.OrdinalIgnoreCase))
        {
            var days = ParseDaysOfWeek(chain.DaysOfWeek, chain.DayOfWeek ?? DayOfWeek.Monday);
            if (!days.Contains(date.DayOfWeek))
                return false;

            var interval = Math.Max(1, chain.Interval);
            if (interval == 1)
                return true;

            var weeksDiff = (StartOfWeekMonday(date).DayNumber - StartOfWeekMonday(anchorDate).DayNumber) / 7;
            return weeksDiff >= 0 && weeksDiff % interval == 0;
        }

        if (freq.Equals(BiWeekly, StringComparison.OrdinalIgnoreCase) ||
            freq.Equals("Bi-Weekly", StringComparison.OrdinalIgnoreCase))
        {
            var days = ParseDaysOfWeek(chain.DaysOfWeek, chain.DayOfWeek ?? DayOfWeek.Monday);
            if (!days.Contains(date.DayOfWeek))
                return false;

            var weeksDiff = (StartOfWeekMonday(date).DayNumber - StartOfWeekMonday(anchorDate).DayNumber) / 7;
            return weeksDiff >= 0 && weeksDiff % 2 == 0;
        }

        if (freq.Equals(Monthly, StringComparison.OrdinalIgnoreCase))
        {
            var interval = Math.Max(1, chain.Interval);
            if (interval > 1)
            {
                var monthsDiff = ((date.Year - anchorDate.Year) * 12) + (date.Month - anchorDate.Month);
                if (monthsDiff < 0 || monthsDiff % interval != 0)
                    return false;
            }

            var targetDay = Math.Clamp(chain.DayOfMonth ?? 1, 1, 31);
            var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            var effectiveDay = Math.Min(targetDay, daysInMonth);
            return date.Day == effectiveDay;
        }

        return false;
    }

    public static DateOnly? GetNextRunDate(RecurringChain chain, DateOnly today, int maxLookaheadDays = 400)
    {
        if (!chain.IsActive)
            return null;

        var startCandidate = chain.LastRunDate.HasValue && chain.LastRunDate.Value >= today
            ? chain.LastRunDate.Value.AddDays(1)
            : today;

        if (chain.StartDate.HasValue && chain.StartDate.Value > startCandidate)
            startCandidate = chain.StartDate.Value;

        for (var i = 0; i < maxLookaheadDays; i++)
        {
            var candidate = startCandidate.AddDays(i);
            if (IsDueOn(chain, candidate))
                return candidate;
        }

        return null;
    }

    public static string GetScheduleSummary(RecurringChain chain)
    {
        var freq = chain.Frequency ?? Daily;
        var interval = Math.Max(1, chain.Interval);

        if (freq.Equals(Daily, StringComparison.OrdinalIgnoreCase))
        {
            var days = ParseDaysOfWeek(chain.DaysOfWeek);
            if (days.Count > 0 && days.Count < 7)
                return $"Daily ({FormatDayListSummary(days)})";

            return interval == 1 ? "Every Day" : $"Every {interval} Days";
        }

        if (freq.Equals(Weekly, StringComparison.OrdinalIgnoreCase))
        {
            var days = ParseDaysOfWeek(chain.DaysOfWeek, chain.DayOfWeek ?? DayOfWeek.Monday);
            var daySummary = FormatDayListSummary(days);
            return interval == 1 ? $"Weekly ({daySummary})" : $"Every {interval} Weeks ({daySummary})";
        }

        if (freq.Equals(BiWeekly, StringComparison.OrdinalIgnoreCase) ||
            freq.Equals("Bi-Weekly", StringComparison.OrdinalIgnoreCase))
        {
            var days = ParseDaysOfWeek(chain.DaysOfWeek, chain.DayOfWeek ?? DayOfWeek.Monday);
            return $"Bi-Weekly ({FormatDayListSummary(days)})";
        }

        if (freq.Equals(Monthly, StringComparison.OrdinalIgnoreCase))
        {
            var dayText = chain.DayOfMonth == 31 ? "Last Day" : $"Day {chain.DayOfMonth ?? 1}";
            return interval switch
            {
                1 => $"Monthly ({dayText})",
                3 => $"Quarterly ({dayText})",
                12 => $"Yearly ({dayText})",
                _ => $"Every {interval} Months ({dayText})"
            };
        }

        return freq;
    }

    private static DateOnly StartOfWeekMonday(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
    }

    private static string FormatDayListSummary(List<DayOfWeek> days)
    {
        if (days.Count == 0)
            return "Mon";

        var set = days.Select(d => (int)d).ToHashSet();
        if (set.Count == 5 && set.SetEquals([1, 2, 3, 4, 5]))
            return "Mon–Fri";
        if (set.Count == 6 && set.SetEquals([1, 2, 3, 4, 5, 6]))
            return "Mon–Sat";
        if (set.Count == 5 && set.SetEquals([0, 1, 2, 3, 4]))
            return "Sun–Thu";

        return string.Join(", ", days.OrderBy(d => (int)d).Select(d => ShortDayNames[(int)d]));
    }
}
