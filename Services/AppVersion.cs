using System.Reflection;
using System.Text.RegularExpressions;

namespace LocalMock.Services;

internal static class AppVersion
{
    private static readonly Regex LeadingV = new(@"^v", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Current
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informational))
            {
                var plusIndex = informational.IndexOf('+');
                return plusIndex >= 0 ? informational[..plusIndex] : informational;
            }

            return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        }
    }

    public static string Normalize(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "0.0.0";
        }

        return LeadingV.Replace(version.Trim(), string.Empty);
    }

    public static bool IsNewer(string? candidate, string? current)
    {
        if (!Version.TryParse(Normalize(candidate), out var candidateVersion))
        {
            return false;
        }

        if (!Version.TryParse(Normalize(current), out var currentVersion))
        {
            return true;
        }

        return candidateVersion > currentVersion;
    }
}
