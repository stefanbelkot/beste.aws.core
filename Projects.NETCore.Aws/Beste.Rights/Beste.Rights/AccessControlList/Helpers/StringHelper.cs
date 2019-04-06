namespace Beste.Rights
{
    public static class StringHelper
    {
        public static string Sanitize(string value) { return string.IsNullOrEmpty(value) ? null : value.Trim().ToLower(); }

        internal static object Quote(string value) { return value.Replace("'", "''"); }
    }
}