using HomeCharts.Domain.Categorization;
using HomeCharts.Domain.Model;

namespace HomeCharts.Domain.Tests;

[TestClass]
public sealed class DomainModelTests
{
    [TestMethod]
    public void NewCategory_HasExpectedDefaults()
    {
        var category = new Category
        {
            Name = "Grocery"
        };

        Assert.AreNotEqual(Guid.Empty, category.Id);
        Assert.AreEqual("Grocery", category.Name);
        Assert.AreEqual("#64748B", category.ColorHex);
        Assert.IsFalse(category.IsSystem);
    }

    [TestMethod]
    public void NewRule_DefaultsToContainsAndActive()
    {
        var rule = new CategorizationRule
        {
            Name = "UBER",
            Pattern = "UBER",
            CategoryId = Guid.NewGuid()
        };

        Assert.AreEqual(RuleMatchType.Contains, rule.MatchType);
        Assert.IsTrue(rule.IsActive);
    }

    [TestMethod]
    public void CategorizationEngine_PrefersExactOverRegexAndContains()
    {
        var groceries = Guid.NewGuid();
        var tech = Guid.NewGuid();
        var misc = Guid.NewGuid();

        var rules = new[]
        {
            new CategorizationRule
            {
                Id = Guid.NewGuid(),
                Name = "Contains OpenAI",
                Pattern = "OPENAI",
                MatchType = RuleMatchType.Contains,
                Priority = 100,
                CategoryId = misc
            },
            new CategorizationRule
            {
                Id = Guid.NewGuid(),
                Name = "Regex OpenAI",
                Pattern = "OPENAI\\s*\\*CHATGPT",
                MatchType = RuleMatchType.Regex,
                Priority = 1,
                CategoryId = tech
            },
            new CategorizationRule
            {
                Id = Guid.NewGuid(),
                Name = "Exact OpenAI Statement",
                Pattern = "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO",
                MatchType = RuleMatchType.Exact,
                Priority = 0,
                CategoryId = groceries
            }
        };

        var result = CategorizationEngine.Match(
            "COMPRA TARJ. 5402XXXXXXXX7020 OPENAI *CHATGPT SUBSCR-SAN FRANCISCO",
            rules);

        Assert.IsNotNull(result);
        Assert.AreEqual(RuleMatchType.Exact, result.MatchType);
        Assert.AreEqual(groceries, result.CategoryId);
    }

    [TestMethod]
    public void CategorizationEngine_UsesPriorityThenLexicalTieBreakWithinMatchType()
    {
        var groceries = Guid.NewGuid();
        var utilities = Guid.NewGuid();

        var highPriorityRule = new CategorizationRule
        {
            Id = Guid.NewGuid(),
            Name = "Zeta Salary",
            Pattern = "NOMINA",
            MatchType = RuleMatchType.Contains,
            Priority = 99,
            CategoryId = groceries
        };

        var lexicalFirstRule = new CategorizationRule
        {
            Id = Guid.NewGuid(),
            Name = "Alpha Salary",
            Pattern = "NOMINA",
            MatchType = RuleMatchType.Contains,
            Priority = 50,
            CategoryId = utilities
        };

        var firstResult = CategorizationEngine.Match(
            "TRANSFERENCIA RECIBIDA DE NOMINA MAY 2025 ADMN. Y SERV. DE PERSONAL- MADRID",
            [lexicalFirstRule, highPriorityRule]);

        Assert.IsNotNull(firstResult);
        Assert.AreEqual(highPriorityRule.Id, firstResult.RuleId);

        var samePriorityRuleA = lexicalFirstRule with { Priority = 50, Name = "Beta Salary" };
        var samePriorityRuleB = lexicalFirstRule with { Id = Guid.NewGuid(), Priority = 50, Name = "Alpha Salary" };

        var secondResult = CategorizationEngine.Match(
            "TRANSFERENCIA RECIBIDA DE NOMINA MAY 2025 ADMN. Y SERV. DE PERSONAL- MADRID",
            [samePriorityRuleA, samePriorityRuleB]);

        Assert.IsNotNull(secondResult);
        Assert.AreEqual(samePriorityRuleB.Id, secondResult.RuleId);
    }

    [TestMethod]
    public void DescriptionNormalizer_UppercasesAndCollapsesWhitespace()
    {
        var normalized = DescriptionNormalizer.Normalize("  compra\t  tarj.   openai  ");

        Assert.AreEqual("COMPRA TARJ. OPENAI", normalized);
    }
}
