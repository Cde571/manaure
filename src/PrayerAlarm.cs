using System;
using System.Globalization;

namespace Gili
{
    /// <summary>One daily prayer alarm at 15:00 in the user's chosen time zone.</summary>
    static class PrayerAlarm
    {
        public static TimeZoneInfo ResolveZone(string id)
        {
            // An empty id follows Windows, including changes to its time zone and daylight saving time.
            if (!string.IsNullOrEmpty(id))
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            return TimeZoneInfo.Local;
        }

        public static DateTime Next(DateTime clockNow)
        {
            DateTime alarm = clockNow.Date.AddHours(15);
            return alarm > clockNow ? alarm : alarm.AddDays(1);
        }

        public static bool IsDue(DateTime clockNow, string lastDay, out string dayKey)
        {
            dayKey = clockNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            DateTime alarm = clockNow.Date.AddHours(15);
            // Allow a late timer tick, but do not replay a missed alarm hours after waking the PC.
            return dayKey != lastDay && clockNow >= alarm && clockNow < alarm.AddMinutes(2);
        }
    }
}
