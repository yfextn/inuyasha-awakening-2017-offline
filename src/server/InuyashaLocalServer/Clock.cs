using System;

namespace InuyashaLocalServer;

internal static class Clock
{
	private static readonly DateTime EpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static long NowMs()
	{
		return (long)(DateTime.UtcNow - EpochUtc).TotalMilliseconds;
	}

	public static long LocalDayStartMs(long nowMs)
	{
		DateTime dateTime = TimeZone.CurrentTimeZone.ToLocalTime(EpochUtc);
		DateTime dateTime2 = dateTime.AddMilliseconds(nowMs);
		return (long)(new DateTime(dateTime2.Year, dateTime2.Month, dateTime2.Day) - dateTime).TotalMilliseconds;
	}
}
