using HomeCharts.Application.Import;
using HomeCharts.Application.UseCases;
using HomeCharts.Domain.Model;

namespace HomeCharts.Application.Tests;

[TestClass]
public sealed class PotentialRuleDraftTests
{
    private static readonly Guid NoneId = Guid.NewGuid();
    private static readonly Guid EducationId = Guid.NewGuid();

    [TestMethod]
    public void IsModified_MatrixOfDescriptionAndCategoryChanges()
    {
        var baseline = new PotentialRuleDraft("OPENAI *CHATGPT SUBSCR", NoneId, "OPENAI *CHATGPT SUBSCR", NoneId);
        Assert.IsFalse(baseline.IsModified, "untouched draft");

        Assert.IsTrue((baseline with { Pattern = "OPENAI" }).IsModified, "description changed");
        Assert.IsTrue((baseline with { CategoryId = EducationId }).IsModified, "category changed");
        Assert.IsTrue((baseline with { Pattern = "OPENAI", CategoryId = EducationId }).IsModified, "both changed");

        var edited = baseline with { Pattern = "OPENAI", CategoryId = EducationId };
        var restored = edited with { Pattern = baseline.SuggestedPattern, CategoryId = baseline.SuggestedCategoryId };
        Assert.IsFalse(restored.IsModified, "both back to baseline");
    }

    [TestMethod]
    public void IsModified_PatternComparisonIsCaseSensitive()
    {
        var draft = new PotentialRuleDraft("OpenAI", NoneId, "OPENAI", NoneId);

        Assert.IsTrue(draft.IsModified);
    }

    [TestMethod]
    public void IsApplicable_RequiresModificationAndACategory()
    {
        var baseline = new PotentialRuleDraft("OPENAI", NoneId, "OPENAI", NoneId);

        Assert.IsFalse(baseline.IsApplicable, "unmodified with category");
        Assert.IsTrue((baseline with { Pattern = "OPEN" }).IsApplicable, "modified with category");
        Assert.IsFalse((baseline with { CategoryId = null }).IsApplicable, "modified but category cleared");
        Assert.IsTrue((baseline with { CategoryId = null }).IsModified, "cleared category still counts as a modification");
    }

    [TestMethod]
    public void IsApplicable_NullBaselineCategoryUntouched_IsNotApplicable()
    {
        var draft = new PotentialRuleDraft("OPENAI", null, "OPENAI", null);

        Assert.IsFalse(draft.IsModified);
        Assert.IsFalse(draft.IsApplicable);
    }

    [TestMethod]
    public void FromRow_StartsAtBaselineAndUnmodified()
    {
        var row = new PotentialRuleRow("k", 1, new DateOnly(2025, 6, 30), "COMPRA TARJ. OPENAI", -20.72m, "OPENAI", NoneId);

        var draft = PotentialRuleDraft.FromRow(row);

        Assert.AreEqual("OPENAI", draft.Pattern);
        Assert.AreEqual(NoneId, draft.CategoryId);
        Assert.IsFalse(draft.IsModified);
    }
}

[TestClass]
public sealed class PreviewPotentialRulesUseCaseSuggestionTests
{
    private const string OpenAiLine = "30/06/2025|COMPRA TARJ. OPENAI|30/06/2025|-20.72|22482.40||5402__7020";

    [TestMethod]
    public async Task ExecuteAsync_SuggestedPatternIsPrefixStrippedAndRawDescriptionUntouched()
    {
        var repositories = new TestRepositories();
        await repositories.PrefixFilters.AddAsync("COMPRA TARJ.");

        var result = await CreateUseCase(repositories).ExecuteAsync(OpenAiLine);

        var row = result.PotentialRows.Single();
        Assert.AreEqual("OPENAI", row.SuggestedPattern);
        Assert.AreEqual("COMPRA TARJ. OPENAI", row.RawDescription);
    }

    [TestMethod]
    public async Task ExecuteAsync_DefaultCategoryIsNoneWhenPresent()
    {
        var repositories = new TestRepositories();
        var grocery = await AddCategoryAsync(repositories, "Grocery");
        var none = await AddCategoryAsync(repositories, "None");

        var result = await CreateUseCase(repositories).ExecuteAsync(OpenAiLine);

        Assert.AreEqual(none.Id, result.PotentialRows.Single().SuggestedCategoryId);
        Assert.AreNotEqual(grocery.Id, result.PotentialRows.Single().SuggestedCategoryId);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithoutNone_DefaultsToFirstAssignableInCatalogOrder()
    {
        var repositories = new TestRepositories();
        await AddCategoryAsync(repositories, "Car");
        var grocery = await AddCategoryAsync(repositories, "Grocery");

        var result = await CreateUseCase(repositories).ExecuteAsync(OpenAiLine);

        // Catalog order puts Grocery before Car even though Car was stored first.
        Assert.AreEqual(grocery.Id, result.PotentialRows.Single().SuggestedCategoryId);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithOnlyNonAssignableCategories_FallsBackToFirstStoredCategory()
    {
        var repositories = new TestRepositories();
        var first = await AddCategoryAsync(repositories, "Uncategorized");
        await AddCategoryAsync(repositories, "Custom");

        var result = await CreateUseCase(repositories).ExecuteAsync(OpenAiLine);

        Assert.AreEqual(first.Id, result.PotentialRows.Single().SuggestedCategoryId);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNoCategories_SuggestedCategoryIsNull()
    {
        var result = await CreateUseCase(new TestRepositories()).ExecuteAsync(OpenAiLine);

        Assert.IsNull(result.PotentialRows.Single().SuggestedCategoryId);
    }

    [TestMethod]
    public async Task ExecuteAsync_RowKey_IsDeterministicAcrossRunsAndDistinctPerLine()
    {
        var repositories = new TestRepositories();
        var useCase = CreateUseCase(repositories);
        var content = OpenAiLine + "\n" + OpenAiLine;

        var first = await useCase.ExecuteAsync(content);
        var second = await useCase.ExecuteAsync(content);

        Assert.AreEqual(2, first.PotentialRows.Count);
        CollectionAssert.AreEqual(
            first.PotentialRows.Select(static row => row.RowKey).ToList(),
            second.PotentialRows.Select(static row => row.RowKey).ToList());
        Assert.AreNotEqual(first.PotentialRows[0].RowKey, first.PotentialRows[1].RowKey, "identical text on different lines needs distinct keys");
        Assert.IsTrue(first.PotentialRows.All(static row => !string.IsNullOrEmpty(row.RowKey)));
    }

    [TestMethod]
    public async Task ExecuteAsync_ParseErrors_ReturnNoRowsAndReportTheErrors()
    {
        var result = await CreateUseCase(new TestRepositories()).ExecuteAsync("not|a|valid|row");

        Assert.IsTrue(result.Errors.Count > 0);
        Assert.AreEqual(0, result.PotentialRows.Count);
    }

    [TestMethod]
    [DataRow(RuleMatchType.Exact, "COMPRA TARJ. OPENAI")]
    [DataRow(RuleMatchType.Regex, "OPEN.I$")]
    [DataRow(RuleMatchType.Contains, "OPENAI")]
    public async Task ExecuteAsync_RowsCoveredByAnyRuleType_AreExcluded(RuleMatchType matchType, string pattern)
    {
        var repositories = new TestRepositories();
        await repositories.Rules.UpsertAsync(new CategorizationRule
        {
            Name = $"{matchType} rule",
            Pattern = pattern,
            MatchType = matchType,
            Priority = 1,
            CategoryId = Guid.NewGuid(),
            IsActive = true
        });

        var result = await CreateUseCase(repositories).ExecuteAsync(OpenAiLine);

        Assert.AreEqual(0, result.PotentialRows.Count);
    }

    [TestMethod]
    public async Task ExecuteAsync_InactiveRule_DoesNotCoverRow()
    {
        var repositories = new TestRepositories();
        await repositories.Rules.UpsertAsync(new CategorizationRule
        {
            Name = "Contains OPENAI",
            Pattern = "OPENAI",
            MatchType = RuleMatchType.Contains,
            CategoryId = Guid.NewGuid(),
            IsActive = false
        });

        var result = await CreateUseCase(repositories).ExecuteAsync(OpenAiLine);

        Assert.AreEqual(1, result.PotentialRows.Count);
    }

    private static PreviewPotentialRulesUseCase CreateUseCase(TestRepositories repositories)
        => new(repositories.Rules, repositories.Categories, repositories.PrefixFilters, new BankStatementParser());

    private static async Task<Category> AddCategoryAsync(TestRepositories repositories, string name)
    {
        var category = new Category { Name = name };
        await repositories.Categories.UpsertAsync(category);
        return category;
    }
}

[TestClass]
public sealed class ApplyPotentialRulesUseCaseTests
{
    private static readonly Guid GroceryId = Guid.NewGuid();
    private static readonly Guid ServicesId = Guid.NewGuid();

    [TestMethod]
    public async Task SaveOneAsync_Created_IsContainsPriorityFiveWithTrimmedPatternAndName()
    {
        var repositories = new TestRepositories();
        var useCase = CreateUseCase(repositories);

        var outcome = await useCase.SaveOneAsync(Draft("  MERCADONA  ", GroceryId));

        Assert.AreEqual(PotentialRuleSaveOutcome.Created, outcome);
        var rule = (await repositories.Rules.GetActiveAsync()).Single();
        Assert.AreEqual(RuleMatchType.Contains, rule.MatchType);
        Assert.AreEqual(5, rule.Priority);
        Assert.AreEqual("MERCADONA", rule.Pattern);
        Assert.AreEqual("Contains MERCADONA", rule.Name);
        Assert.AreEqual(GroceryId, rule.CategoryId);
        Assert.IsTrue(rule.IsActive);
    }

    [TestMethod]
    public async Task SaveOneAsync_UnmodifiedDraftWithCategory_StillSaves()
    {
        var repositories = new TestRepositories();
        var draft = new PotentialRuleDraft("OPENAI", GroceryId, "OPENAI", GroceryId);

        var outcome = await CreateUseCase(repositories).SaveOneAsync(draft);

        Assert.AreEqual(PotentialRuleSaveOutcome.Created, outcome);
    }

    [TestMethod]
    public async Task SaveOneAsync_SameCategoryAndPatternIgnoringCase_IsDuplicate()
    {
        var repositories = new TestRepositories();
        var useCase = CreateUseCase(repositories);
        await useCase.SaveOneAsync(Draft("OPENAI", ServicesId));

        var outcome = await useCase.SaveOneAsync(Draft("openai", ServicesId));

        Assert.AreEqual(PotentialRuleSaveOutcome.Duplicate, outcome);
        Assert.AreEqual(1, (await repositories.Rules.GetActiveAsync()).Count);
    }

    [TestMethod]
    public async Task SaveOneAsync_BlankPattern_IsInvalidAndWritesNothing()
    {
        var repositories = new TestRepositories();

        var outcome = await CreateUseCase(repositories).SaveOneAsync(Draft("   ", GroceryId));

        Assert.AreEqual(PotentialRuleSaveOutcome.InvalidPattern, outcome);
        Assert.AreEqual(0, (await repositories.Rules.GetActiveAsync()).Count);
    }

    [TestMethod]
    public async Task SaveOneAsync_NullCategory_IsCategoryMissingAndWritesNothing()
    {
        var repositories = new TestRepositories();

        var outcome = await CreateUseCase(repositories).SaveOneAsync(Draft("MERCADONA", null));

        Assert.AreEqual(PotentialRuleSaveOutcome.CategoryMissing, outcome);
        Assert.AreEqual(0, (await repositories.Rules.GetActiveAsync()).Count);
    }

    [TestMethod]
    public async Task SaveModifiedAsync_CountsCreatedDuplicateAndInvalid()
    {
        var repositories = new TestRepositories();
        await repositories.Rules.UpsertAsync(new CategorizationRule
        {
            Name = "Contains OPENAI",
            Pattern = "OPENAI",
            MatchType = RuleMatchType.Contains,
            Priority = 5,
            CategoryId = ServicesId,
            IsActive = true
        });

        var result = await CreateUseCase(repositories).SaveModifiedAsync(
        [
            Draft("MERCADONA", GroceryId),
            Draft("OPENAI", ServicesId),
            Draft("   ", GroceryId)
        ]);

        Assert.AreEqual(3, result.ConsideredCount);
        Assert.AreEqual(1, result.CreatedCount);
        Assert.AreEqual(1, result.DuplicateCount);
        Assert.AreEqual(1, result.InvalidCount);
    }

    [TestMethod]
    public async Task SaveModifiedAsync_SkipsUnmodifiedAndCategorylessDrafts()
    {
        var repositories = new TestRepositories();
        var unmodified = new PotentialRuleDraft("KEEP", GroceryId, "KEEP", GroceryId);
        var noCategory = new PotentialRuleDraft("NOCAT", null, "NOCAT EDITED", null);

        var result = await CreateUseCase(repositories).SaveModifiedAsync([unmodified, noCategory]);

        Assert.AreEqual(0, result.ConsideredCount);
        Assert.AreEqual(0, result.CreatedCount);
        Assert.AreEqual(0, (await repositories.Rules.GetActiveAsync()).Count);
    }

    [TestMethod]
    public async Task SaveModifiedAsync_SavesInInputOrderAndSameBatchRepeatIsDuplicate()
    {
        var repositories = new TestRepositories();

        var result = await CreateUseCase(repositories).SaveModifiedAsync(
        [
            Draft("ZETA", GroceryId),
            Draft("ALPHA", GroceryId),
            Draft("ZETA", GroceryId)
        ]);

        Assert.AreEqual(3, result.ConsideredCount);
        Assert.AreEqual(2, result.CreatedCount);
        Assert.AreEqual(1, result.DuplicateCount);
        CollectionAssert.AreEqual(
            new[] { "ZETA", "ALPHA" },
            (await repositories.Rules.GetActiveAsync()).Select(static rule => rule.Pattern).ToList());
    }

    [TestMethod]
    public async Task SaveModifiedAsync_ManualOverrideOnTransaction_IsUnchanged()
    {
        // Guardrail 2 evidence: creating rules must not re-apply rules over a manual choice.
        var repositories = new TestRepositories();
        var manualCategoryId = Guid.NewGuid();
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            BookingDate = new DateOnly(2025, 6, 30),
            Description = "COMPRA TARJ. OPENAI",
            NormalizedDescription = "COMPRA TARJ. OPENAI",
            Amount = -20.72m,
            CategoryId = manualCategoryId,
            TransactionFingerprint = "fp-manual"
        };
        await repositories.Transactions.UpsertManyAsync([transaction]);
        await repositories.ManualOverrides.AddAsync(new ManualOverride
        {
            TransactionId = transaction.Id,
            CategoryId = manualCategoryId,
            Reason = "Corrected by user"
        });

        var result = await CreateUseCase(repositories).SaveModifiedAsync([Draft("OPENAI", GroceryId)]);

        Assert.AreEqual(1, result.CreatedCount, "the rule that would match the transaction was created");
        var stored = await repositories.Transactions.GetByIdAsync(transaction.Id);
        Assert.AreEqual(manualCategoryId, stored!.CategoryId);
        var overrides = await repositories.ManualOverrides.GetByTransactionIdAsync(transaction.Id);
        Assert.AreEqual(1, overrides.Count);
        Assert.AreEqual(manualCategoryId, overrides[0].CategoryId);
        Assert.AreEqual("Corrected by user", overrides[0].Reason);
    }

    private static ApplyPotentialRulesUseCase CreateUseCase(TestRepositories repositories)
        => new(new CreateCategorizationRuleUseCase(repositories.Rules));

    // A modified draft: baseline differs from the current pattern, so IsModified holds.
    private static PotentialRuleDraft Draft(string pattern, Guid? categoryId)
        => new("baseline", categoryId, pattern, categoryId);
}
