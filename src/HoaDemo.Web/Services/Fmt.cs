using System.Globalization;

namespace HoaDemo.Web.Services;

/// <summary>Display formatting that ignores the server culture.</summary>
public static class Fmt
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public static string Money(decimal value) => value.ToString("C", EnUs);
    public static string Date(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public static string Percent(decimal fraction) => (fraction * 100m).ToString("0.##", EnUs) + "%";
}
