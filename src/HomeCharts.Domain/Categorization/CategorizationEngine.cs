using System.Text.RegularExpressions;
using HomeCharts.Domain.Model;

namespace HomeCharts.Domain.Categorization;

public static class CategorizationEngine
{
    public static CategorizationMatch? Match(string description, IReadOnlyCollection<CategorizationRule> activeRules)
    {
        if (string.IsNullOrWhiteSpace(description) || activeRules.Count == 0)
        {
            return null;
        }

        var candidates = activeRules
            .Where(static rule => rule.IsActive)
            .Select(rule => CreateCandidate(rule, description))
            .Where(static candidate => candidate is not null)
            .Cast<Candidate>()
            .OrderByDescending(static candidate => candidate.MatchRank)
            .ThenByDescending(static candidate => candidate.Rule.Priority)
            .ThenBy(static candidate => candidate.Rule.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.Rule.Pattern, StringComparer.Ordinal)
            .ThenBy(static candidate => candidate.Rule.Id)
            .ToArray();

        if (candidates.Length == 0)
        {
            return null;
        }

        var winner = candidates[0].Rule;
        return new CategorizationMatch(
            winner.CategoryId,
            winner.Id,
            winner.Name,
            winner.MatchType,
            winner.Priority);
    }

    private static Candidate? CreateCandidate(CategorizationRule rule, string description)
    {
        var pattern = rule.Pattern?.Trim();
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        return rule.MatchType switch
        {
            RuleMatchType.Exact when string.Equals(description, pattern, StringComparison.OrdinalIgnoreCase)
                => new Candidate(rule, 3),
            RuleMatchType.Regex when IsRegexMatch(description, pattern)
                => new Candidate(rule, 2),
            RuleMatchType.Contains when description.Contains(pattern, StringComparison.OrdinalIgnoreCase)
                => new Candidate(rule, 1),
            _ => null
        };
    }

    private static bool IsRegexMatch(string description, string pattern)
    {
        try
        {
            return Regex.IsMatch(description, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private sealed record Candidate(CategorizationRule Rule, int MatchRank);
}

public sealed record CategorizationMatch(
    Guid CategoryId,
    Guid RuleId,
    string RuleName,
    RuleMatchType MatchType,
    int Priority);
