namespace HomeCharts.Domain.Categorization;

public static class DescriptionPrefixStripper
{
    public static string Strip(string description, IEnumerable<string> prefixes)
    {
        var orderedPrefixes = prefixes
            .Where(static prefix => !string.IsNullOrWhiteSpace(prefix))
            .OrderByDescending(static prefix => prefix.Length)
            .ToArray();

        var value = description;
        var changed = true;
        while (changed && orderedPrefixes.Length > 0)
        {
            changed = false;
            foreach (var prefix in orderedPrefixes)
            {
                if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = value[prefix.Length..].TrimStart(' ', '-', ':');
                changed = true;
                break;
            }
        }

        return string.IsNullOrWhiteSpace(value) ? description : value;
    }
}
