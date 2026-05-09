namespace SmartShop.Utility
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo VnZone =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public static DateTime VietnamNow() =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VnZone);
    }
}
