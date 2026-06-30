using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Infra.Utility
{
    public static class Helper
    {
        public static string GetFileExtension(this string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            return Path.GetExtension(fileName)?.TrimStart('.').ToLower() ?? string.Empty;
        }

        public static string AddCommaToCommaString(this string cmstring)
        {
            return $@",{cmstring},";
        }
        public static string RemoveCommaFromCommaString(this string cmstring)
        {
            return cmstring.Substring(1, cmstring.Length - 2);
        }
        public static string getUniqueCode()
        {
            return Guid.NewGuid().ToString("N");
        }

        public static string getUniqueNumber()
        {
            byte[] buffer = Guid.NewGuid().ToByteArray();
            string uniquecode = BitConverter.ToUInt32(buffer, 12).ToString();
            return uniquecode;
        }

        public static DateTime getUtcTime()
        {
            return DateTime.UtcNow;
        }

        public static DateTime getMMTime()
        {
            DateTime utc = DateTime.UtcNow;
            return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("Myanmar Standard Time"));
        }

        public static DateTime getLocalTime()
        {
            DateTime utc = DateTime.UtcNow;
            return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("Myanmar Standard Time"));
        }

        public static DateTime getLocalTime(DateTime? date = null)
        {
            DateTime utc = date ?? DateTime.UtcNow;
            return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("Myanmar Standard Time"));
        }

        public static string getDateAgo(DateTime dateTime)
        {
            TimeSpan timeSpan = DateTime.Now.Subtract(dateTime);

            if (timeSpan.TotalSeconds < 0)
            {
                return "just now";
            }

            return timeSpan.TotalSeconds switch
            {
                <= 60 => $"{timeSpan.Seconds} sec{(timeSpan.Seconds == 1 ? "" : "s")}",

                _ => timeSpan.TotalMinutes switch
                {
                    <= 1 => "1 min",
                    < 60 => $"{timeSpan.Minutes} min{(timeSpan.Minutes == 1 ? "" : "s")}",
                    _ => timeSpan.TotalHours switch
                    {
                        <= 1 => "1 hr",
                        < 24 => $"{timeSpan.Hours} hr{(timeSpan.Hours == 1 ? "" : "s")}",
                        _ => timeSpan.TotalDays switch
                        {
                            <= 1 => "1 day",
                            <= 30 => $"{timeSpan.Days} day{(timeSpan.Days == 1 ? "" : "s")}",

                            <= 60 => "1 month",
                            < 365 => $"{Math.Floor(timeSpan.Days / 30.0)} month{(Math.Floor(timeSpan.Days / 30.0) == 1 ? "" : "s")}",

                            <= 365 * 2 => "1 yr",
                            _ => $"{Math.Floor(timeSpan.Days / 365.0)} yr{(Math.Floor(timeSpan.Days / 365.0) == 1 ? "" : "s")}"
                        }
                    }
                }
            };
        }

        public static T Convert<T>(object input)
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter.CanConvertFrom(input.GetType()))
            {
                return (T)converter.ConvertFrom(input);
            }
            return default(T);
        }

        public static List<T> CloneList<T>(this IEnumerable<T> source) where T : class, new()
        {
            return source.Select(item => item.Clone()).ToList();
        }

        public static T Clone<T>(this T source) where T : class, new()
        {
            var dest = new T();
            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.CanWrite)
                {
                    property.SetValue(dest, property.GetValue(source));
                }
            }

            return dest;
        }

        public static List<T> DetectChanges<T>(List<T> modifiedEntries, List<T> originalEntries, params Func<T, object>[] propertiesToCompare)
        {
            if (modifiedEntries.Count != originalEntries.Count)
                throw new ArgumentException("The lists must have the same number of elements.");

            return modifiedEntries
                .Zip(originalEntries, (modified, original) => new { Modified = modified, Original = original })
                .Where(pair => propertiesToCompare.Any(prop =>
                {
                    var modifiedValue = prop(pair.Modified);
                    var originalValue = prop(pair.Original);

                    return modifiedValue != null ? !modifiedValue.Equals(originalValue) : originalValue != null;
                }))
                .Select(pair => pair.Modified)
                .ToList();
        }

        public static string getUnixTime()
        {
            DateTime foo = Helper.getLocalTime();
            string unixTime = ((DateTimeOffset)foo).ToUnixTimeSeconds().ToString();
            return unixTime;
        }

        public static string RemoveDecimalPrecision(decimal? data)
        {
            string formattedClubScore = data?.ToString("N0");
            return formattedClubScore ?? "";
        }

        public static string GetCacheKeyFormattedDateString(DateTime? date)
        {
            return $"{date?.ToString("dd-MMM-yyyy")}";
        }

        public static DateTime getExpireMonth(int month)
        {
            DateTime date = getLocalTime();
            DateTime expiredDate = date.AddMonths(month);
            return expiredDate;
        }

        public static DateTime getExpireDate(int day)
        {
            DateTime date = getLocalTime();
            DateTime expiredDate = date.AddDays(day);
            return expiredDate;
        }

        public static string MaskString(string source, char maskChar)
        {
            if (source.Length <= 3)
                return source;

            int lengthToMask = source.Length - 3;
            return new string(maskChar, lengthToMask) + source.Substring(lengthToMask);
        }

        public static T SafeDeserialize<T>(string json) where T : new()
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json) ?? new T();
            }
            catch (JsonException)
            {
                return new T();
            }
        }

        public static string SafeSerialize<T>(T obj)
        {
            try
            {
                return JsonSerializer.Serialize(obj);
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Unwraps an exception chain and returns every message joined inner-most last.
        /// Wrappers like EF Core's DbUpdateException only say "See the inner exception
        /// for details" — this surfaces the real cause (e.g. the SQL Server error).
        /// </summary>
        public static string GetFullMessage(this Exception? ex)
        {
            var sb = new StringBuilder();
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (sb.Length > 0) sb.Append(" -> ");
                sb.Append(current.Message);
            }
            return sb.ToString();
        }

        public static string CalculateTimeAgo(DateTime? dateTime)
        {
            if (dateTime == null)
                return "";

            var timeSpan = DateTime.UtcNow - dateTime.Value;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m";
            if (timeSpan.TotalHours < 24)
            {
                var hours = (int)timeSpan.TotalHours;
                var minutes = (int)(timeSpan.TotalMinutes % 60);
                return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
            }
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)}w";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)}mo";

            return $"{(int)(timeSpan.TotalDays / 365)}y";
        }
    }
}
