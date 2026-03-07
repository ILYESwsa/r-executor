using System.Text.RegularExpressions;

namespace SafeScriptPlugin;

public static class Entry
{
    public static string Execute(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            return "Plugin received empty script.";
        }

        var matches = Regex.Matches(script, "print\\s*\\((?<val>.+?)\\)", RegexOptions.IgnoreCase);
        if (matches.Count == 0)
        {
            return "Plugin found no print(...) statements.";
        }

        var values = new List<string>();
        foreach (Match match in matches)
        {
            var raw = match.Groups["val"].Value.Trim();
            if ((raw.StartsWith('"') && raw.EndsWith('"')) || (raw.StartsWith('\'') && raw.EndsWith('\'')))
            {
                raw = raw[1..^1];
            }

            if (!string.IsNullOrWhiteSpace(raw))
            {
                values.Add(raw);
            }
        }

        return values.Count == 0
            ? "Plugin extracted only empty print payloads."
            : $"Plugin extracted {values.Count} print payload(s): {string.Join(" | ", values)}";
    }
}
