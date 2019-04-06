using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Beste.Module.ExtensionMethods
{
    static class ExtensionMethods
    {
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
        public static void Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }
        }

        public static bool HasUpperCase(this string text)
        {
            foreach (char c in text)
            {
                if (char.IsUpper(c)) return true;
            }
            return false;
        }
        public static bool HasLowerCase(this string text)
        {

            foreach (char c in text)
            {
                if (char.IsLower(c)) return true;
            }
            return false;
        }
        public static bool HasDigit(this string text)
        {

            foreach (char c in text)
            {
                if (char.IsDigit(c)) return true;
            }
            return false;
        }
        public static bool HasSpecialChars(this string text)
        {

            foreach (char c in text)
            {
                if (!char.IsLetterOrDigit(c)) return true;
            }
            return false;
        }

        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        static CultureInfo CultureInfoDe = new CultureInfo("de-DE");
        /// <summary>
        /// Converts the passed date to a calendar week
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int CalendarWeek(this DateTime date)
        {
            int result = -1;

            DateTimeFormatInfo dateTimeFormatInfo = CultureInfoDe.DateTimeFormat;
            result = dateTimeFormatInfo.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);

            return result;
        }
    }

}
