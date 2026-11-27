using System;
using Gili;

static class PrayerAlarmTests
{
    static int checks;

    static void Check(bool condition, string description)
    {
        checks++;
        if (!condition) throw new Exception(description);
    }

    static DateTime Utc(int month, int day, int hour, int minute)
    {
        return new DateTime(2026, month, day, hour, minute, 0, DateTimeKind.Utc);
    }

    static bool Due(DateTime utc, TimeZoneInfo zone, string lastDay)
    {
        string key;
        return PrayerAlarm.IsDue(TimeZoneInfo.ConvertTimeFromUtc(utc, zone), lastDay, out key);
    }

    static void Main()
    {
        var bogota = PrayerAlarm.ResolveZone("SA Pacific Standard Time");
        Check(!Due(Utc(11, 27, 19, 59), bogota, ""), "Do not ring before 15:00");
        Check(Due(Utc(11, 27, 20, 0), bogota, ""), "Ring at 15:00 in the chosen zone");
        Check(Due(Utc(11, 27, 20, 1), bogota, ""), "Allow a late clock tick");
        Check(!Due(Utc(11, 27, 20, 2), bogota, ""), "Do not replay a missed alarm");
        Check(!Due(Utc(11, 27, 20, 0), bogota, "20261127"), "Persisted day prevents a duplicate after restarting");
        Check(Due(Utc(11, 28, 20, 0), bogota, "20261127"), "Ring again on the next day");
        foreach (int hour in new[] { 3, 6, 12, 18, 21 })
        {
            string key;
            Check(!PrayerAlarm.IsDue(new DateTime(2026, 11, 27, hour, 0, 0), "", out key), "Only the 15:00 alarm exists");
        }

        var eastern = PrayerAlarm.ResolveZone("Eastern Standard Time");
        Check(Due(Utc(3, 7, 20, 0), eastern, ""), "15:00 before spring daylight saving transition");
        Check(Due(Utc(3, 8, 19, 0), eastern, ""), "15:00 after spring daylight saving transition");
        Check(!Due(Utc(3, 8, 20, 0), eastern, ""), "No second alarm at the old UTC time");
        Check(Due(Utc(10, 31, 19, 0), eastern, ""), "15:00 before autumn daylight saving transition");
        Check(Due(Utc(11, 1, 20, 0), eastern, ""), "15:00 after autumn daylight saving transition");
        Check(PrayerAlarm.ResolveZone("").Id == TimeZoneInfo.Local.Id, "Windows time zone is the default");
        Check(PrayerAlarm.ResolveZone("removed-zone").Id == TimeZoneInfo.Local.Id, "Invalid stored zones fall back to Windows");

        var before = new DateTime(2026, 12, 31, 14, 59, 59);
        Check(PrayerAlarm.Next(before) == before.Date.AddHours(15), "Next alarm is today before 15:00");
        Check(PrayerAlarm.Next(before.Date.AddHours(15)) == new DateTime(2027, 1, 1, 15, 0, 0), "Next alarm rolls over to the next year");
        Console.WriteLine("PASS: " + checks + " alarm and time zone checks");
    }
}
