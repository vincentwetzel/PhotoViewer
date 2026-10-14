using System.Globalization;

namespace PhotoViewer.Services;

public static class PhotoDateGrouping
{
    public static string GetLabel(DateTime date, DateTime? today = null)
    {
        if (date == default) return "Date unavailable";

        var localDate = date.ToLocalTime().Date;
        var currentDate = (today ?? DateTime.Today).Date;
        if (localDate == currentDate) return "Today";
        if (localDate == currentDate.AddDays(-1)) return "Yesterday";

        return localDate.Year == currentDate.Year
            ? localDate.ToString("MMMM d", CultureInfo.CurrentCulture)
            : localDate.ToString("MMMM d, yyyy", CultureInfo.CurrentCulture);
    }

    public static string GetKey(DateTime date) => date == default
        ? "unavailable"
        : date.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
