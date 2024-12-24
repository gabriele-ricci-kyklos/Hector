using System;

namespace Hector
{
    public static class TimeExtensionMethods
    {
        public static TimeSpan Milliseconds(this int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
        public static TimeSpan Milliseconds(this double milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
        public static TimeSpan Seconds(this int seconds) => TimeSpan.FromSeconds(seconds);
        public static TimeSpan Seconds(this double seconds) => TimeSpan.FromSeconds(seconds);
        public static TimeSpan Minutes(this int minutes) => TimeSpan.FromMinutes(minutes);
        public static TimeSpan Minutes(this double minutes) => TimeSpan.FromMinutes(minutes);
        public static TimeSpan Hours(this int hours) => TimeSpan.FromMinutes(hours);
        public static TimeSpan Hours(this double hours) => TimeSpan.FromMinutes(hours);
        public static TimeSpan Days(this int days) => TimeSpan.FromMinutes(days);
        public static TimeSpan Days(this double days) => TimeSpan.FromMinutes(days);
        public static TimeSpan And(this TimeSpan sourceTime, TimeSpan offset) => sourceTime.Add(offset);
    }
}
