using System.Globalization;

namespace loginlogout.Helpers;

public static class RupeeFormatter
{
    private static readonly CultureInfo IndianCulture = CultureInfo.GetCultureInfo("en-IN");

    public static string Format(decimal amount) => $"Rs {amount.ToString("N2", IndianCulture)}";
}
