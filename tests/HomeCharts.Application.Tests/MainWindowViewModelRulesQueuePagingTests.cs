using HomeCharts.Domain.Model;
using HomeCharts.Presentation.Mvp;

namespace HomeCharts.Application.Tests;

/// <summary>
/// Coverage for <see cref="PotentialRulesViewModel"/>, the single "Potential New Rules" list
/// (import-rules-redesign). Kept in this file (formerly the MainWindowViewModel rules-queue paging
/// tests) so history follows the paging behaviors that must keep holding: edits survive page turns and
/// Apply All reads every page. The child VM is built directly with a controllable host; there is no
/// HomeCharts.Presentation.Tests project, and Application.Tests already references Presentation.
/// </summary>
[TestClass]
public sealed class PotentialRulesViewModelTests
{
    private const int TwoPageRowCount = PotentialRulesViewModel.PageSize + 10;

    [TestMethod]
    public async Task Paging_EditOnDisplayedPage_SurvivesNavigatingAwayAndBack()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(TwoPageRowCount);
        var viewModel = harness.ViewModel;
        Assert.AreEqual(PotentialRulesViewModel.PageSize, viewModel.Rows.Count, "page 1 should be full");

        var editedRow = viewModel.Rows[0];
        var editedKey = editedRow.RowKey;
        var editedDescription = editedRow.Description;
        editedRow.SelectedCategory = harness.Grocery;
        Assert.IsTrue(editedRow.IsModified, "row should be marked modified immediately after the edit");

        viewModel.NextPageCommand.Execute(null);
        Assert.AreEqual("Page 2/2", viewModel.PageInfo);
        viewModel.PreviousPageCommand.Execute(null);
        Assert.AreEqual("Page 1/2", viewModel.PageInfo);

        var rowAfterRoundTrip = viewModel.Rows[0];
        Assert.AreEqual(editedKey, rowAfterRoundTrip.RowKey, "same logical row should reappear on page 1");
        Assert.AreEqual(editedDescription, rowAfterRoundTrip.Description);
        Assert.AreEqual(harness.Grocery, rowAfterRoundTrip.SelectedCategory, "category edit must survive paging away and back");
        Assert.IsTrue(rowAfterRoundTrip.IsModified, "modified flag must survive paging away and back");
    }

    [TestMethod]
    public async Task ApplyAll_ModifiedRowsOnMultiplePages_AreAllPersistedAsRules()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(TwoPageRowCount);
        var viewModel = harness.ViewModel;

        var page1Row = viewModel.Rows[0];
        var page1Description = page1Row.Description;
        page1Row.SelectedCategory = harness.Grocery;

        viewModel.NextPageCommand.Execute(null);
        Assert.AreEqual("Page 2/2", viewModel.PageInfo);
        var page2Row = viewModel.Rows[0];
        var page2Description = page2Row.Description;
        page2Row.SelectedCategory = harness.Car;

        Assert.IsTrue(viewModel.ApplyAllCommand.CanExecute(null), "Apply All should be enabled with modified rows on two pages");
        viewModel.ApplyAllCommand.Execute(null);
        await harness.LastRun;

        var patterns = (await harness.Repositories.Rules.GetActiveAsync()).Select(static rule => rule.Pattern).ToHashSet(StringComparer.Ordinal);
        Assert.IsTrue(patterns.Contains(page1Description), "page 1's modified row must be applied");
        Assert.IsTrue(patterns.Contains(page2Description), "page 2's modified row must be applied, not just the displayed page");
        Assert.AreEqual(2, patterns.Count);
        Assert.IsTrue(harness.Statuses.Last().Contains("Created: 2", StringComparison.Ordinal), harness.Statuses.Last());
        Assert.AreEqual($"Potential new rules: {TwoPageRowCount - 2}", viewModel.Summary, "applied rows leave the queue");
        Assert.AreEqual(1, harness.DependentsRefreshCount);
    }

    [TestMethod]
    public async Task ApplyAll_CanExecute_TrueForModifiedRowOnAnotherPage()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(TwoPageRowCount);
        var viewModel = harness.ViewModel;
        Assert.IsFalse(viewModel.ApplyAllCommand.CanExecute(null), "nothing modified yet");

        viewModel.Rows[0].SelectedCategory = harness.Grocery;
        viewModel.NextPageCommand.Execute(null);
        Assert.AreEqual("Page 2/2", viewModel.PageInfo);

        Assert.IsTrue(
            viewModel.ApplyAllCommand.CanExecute(null),
            "CanExecute must consider modified rows on every page, not only the one currently displayed");
    }

    [TestMethod]
    public async Task ApplyAll_NothingModified_ReportsModifyAtLeastOneRow()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(3);

        harness.ViewModel.ApplyAllCommand.Execute(null);
        await harness.LastRun;

        Assert.AreEqual("Modify at least one row before applying all rules.", harness.Statuses.Last());
        Assert.AreEqual(0, (await harness.Repositories.Rules.GetActiveAsync()).Count);
    }

    [TestMethod]
    public async Task ApplyAllText_ShowsCountOfApplicableRowsAcrossPages()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(TwoPageRowCount);
        var viewModel = harness.ViewModel;
        Assert.AreEqual("Apply All", viewModel.ApplyAllText);

        viewModel.Rows[0].SelectedCategory = harness.Grocery;
        Assert.AreEqual("Apply All (1)", viewModel.ApplyAllText);

        viewModel.NextPageCommand.Execute(null);
        viewModel.Rows[0].Description = "EDITED PATTERN";
        Assert.AreEqual("Apply All (2)", viewModel.ApplyAllText);

        viewModel.Rows[0].Description = viewModel.Rows[0].Draft.SuggestedPattern;
        Assert.AreEqual("Apply All (1)", viewModel.ApplyAllText, "a row edited back to baseline no longer counts");
    }

    [TestMethod]
    public async Task ApplyAllText_ModifiedRowWithoutCategory_IsNotCounted()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(2);

        harness.ViewModel.Rows[0].SelectedCategory = null;

        Assert.IsTrue(harness.ViewModel.Rows[0].IsModified);
        Assert.AreEqual("Apply All", harness.ViewModel.ApplyAllText);
        Assert.IsFalse(harness.ViewModel.ApplyAllCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Paging_MoreThanOnePageOfRows_ReportsPageCountSummaryAndPageSizedRows()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(130);
        var viewModel = harness.ViewModel;

        Assert.AreEqual("Potential new rules: 130", viewModel.Summary);
        Assert.AreEqual("Page 1/2", viewModel.PageInfo);
        Assert.AreEqual(PotentialRulesViewModel.PageSize, viewModel.Rows.Count);
        Assert.IsFalse(viewModel.PreviousPageCommand.CanExecute(null));
        Assert.IsTrue(viewModel.NextPageCommand.CanExecute(null));

        viewModel.NextPageCommand.Execute(null);

        Assert.AreEqual(130 - PotentialRulesViewModel.PageSize, viewModel.Rows.Count);
        Assert.IsFalse(viewModel.NextPageCommand.CanExecute(null));
        Assert.IsTrue(viewModel.PreviousPageCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Paging_ExactlyOnePageOfRows_HasNoSecondPage()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(PotentialRulesViewModel.PageSize);

        Assert.AreEqual("Page 1/1", harness.ViewModel.PageInfo);
        Assert.IsFalse(harness.ViewModel.NextPageCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task RowSave_CreatesRuleAndSavedRowLeavesTheQueue()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(3);
        var row = harness.ViewModel.Rows[0];
        var savedKey = row.RowKey;
        var savedPattern = row.Description;

        row.SaveCommand.Execute(null);
        await harness.LastRun;

        var rule = (await harness.Repositories.Rules.GetActiveAsync()).Single();
        Assert.AreEqual(savedPattern, rule.Pattern);
        Assert.AreEqual(harness.None.Id, rule.CategoryId, "untouched row saves its suggested category");
        Assert.IsFalse(harness.ViewModel.Rows.Any(candidate => candidate.RowKey == savedKey), "row now matches the rule");
        Assert.AreEqual("Potential new rules: 2", harness.ViewModel.Summary);
        Assert.AreEqual($"Rule saved for '{savedPattern}'.", harness.Statuses.Last());
        Assert.AreEqual(1, harness.DependentsRefreshCount);
    }

    [TestMethod]
    public async Task RowSave_EditOnOtherRows_SurvivesTheRefreshAfterSaving()
    {
        // D-1 ruling: refreshes triggered from inside the list keep edits for rows still queued.
        using var harness = await PotentialRulesHarness.CreateAsync(3);
        var viewModel = harness.ViewModel;
        var savedRow = viewModel.Rows[0];
        var otherRow = viewModel.Rows[1];
        var otherKey = otherRow.RowKey;
        savedRow.Description = harness.UniquePatternOf(savedRow);
        otherRow.Description = harness.UniquePatternOf(otherRow) + " EDITED";
        otherRow.SelectedCategory = harness.Grocery;

        savedRow.SaveCommand.Execute(null);
        await harness.LastRun;

        var survivor = viewModel.Rows.Single(candidate => candidate.RowKey == otherKey);
        Assert.AreEqual(harness.UniquePatternOf(survivor) + " EDITED", survivor.Description);
        Assert.AreEqual(harness.Grocery, survivor.SelectedCategory);
        Assert.IsTrue(survivor.IsModified);
        Assert.AreEqual("Apply All (1)", viewModel.ApplyAllText);
    }

    [TestMethod]
    public async Task RefreshAsyncWithContent_ResetsUnsavedEdits()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(3);
        var viewModel = harness.ViewModel;
        var key = viewModel.Rows[0].RowKey;
        var original = viewModel.Rows[0].Description;
        viewModel.Rows[0].Description = "EDITED";
        viewModel.Rows[0].SelectedCategory = harness.Grocery;

        await viewModel.RefreshAsync(harness.Content);

        var rebuilt = viewModel.Rows.Single(candidate => candidate.RowKey == key);
        Assert.AreEqual(original, rebuilt.Description);
        Assert.AreEqual(harness.None, rebuilt.SelectedCategory);
        Assert.IsFalse(rebuilt.IsModified);
        Assert.AreEqual("Apply All", viewModel.ApplyAllText);
    }

    [TestMethod]
    public async Task RefreshCommand_SameFile_KeepsEditsButChangedFileResetsThem()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(3);
        var viewModel = harness.ViewModel;
        var key = viewModel.Rows[0].RowKey;
        var original = viewModel.Rows[0].Description;
        viewModel.Rows[0].Description = "EDITED";

        var oldRow = viewModel.Rows[0];
        viewModel.RefreshCommand.Execute(null);
        await PotentialRulesHarness.WaitUntilAsync(() => !ReferenceEquals(viewModel.Rows.Single(r => r.RowKey == key), oldRow));
        Assert.AreEqual("EDITED", viewModel.Rows.Single(candidate => candidate.RowKey == key).Description);

        var keptRow = viewModel.Rows.Single(r => r.RowKey == key);
        harness.SwitchToCopyOfFile();
        viewModel.RefreshCommand.Execute(null);
        await PotentialRulesHarness.WaitUntilAsync(() => !ReferenceEquals(viewModel.Rows.Single(r => r.RowKey == key), keptRow));
        Assert.AreEqual(original, viewModel.Rows.Single(candidate => candidate.RowKey == key).Description);
        Assert.IsFalse(viewModel.Rows.Single(candidate => candidate.RowKey == key).IsModified);
    }

    [TestMethod]
    public async Task RowSave_BlankDescription_ReportsInvalidPatternAndCreatesNothing()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(2);
        var row = harness.ViewModel.Rows[0];
        row.Description = "   ";

        row.SaveCommand.Execute(null);
        await harness.LastRun;

        Assert.AreEqual("Description is empty. Enter a matching string first.", harness.Statuses.Last());
        Assert.AreEqual(0, (await harness.Repositories.Rules.GetActiveAsync()).Count);
    }

    [TestMethod]
    public async Task RowSave_ExistingRuleForPatternAndCategory_ReportsDuplicate()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(2);
        await harness.Repositories.Rules.UpsertAsync(new CategorizationRule
        {
            Name = "Contains ZZZ",
            Pattern = "ZZZ",
            MatchType = RuleMatchType.Contains,
            Priority = 5,
            CategoryId = harness.None.Id,
            IsActive = true
        });
        var row = harness.ViewModel.Rows[0];
        row.Description = "ZZZ";

        row.SaveCommand.Execute(null);
        await harness.LastRun;

        Assert.AreEqual("Matching rule already exists.", harness.Statuses.Last());
        Assert.AreEqual(1, (await harness.Repositories.Rules.GetActiveAsync()).Count);
    }

    [TestMethod]
    public async Task RowSaveCommand_DisabledWithoutCategoryAndWhileBusy()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(2);
        var row = harness.ViewModel.Rows[0];
        Assert.IsTrue(row.SaveCommand.CanExecute(null));

        row.SelectedCategory = null;
        Assert.IsFalse(row.SaveCommand.CanExecute(null), "no category");

        row.SelectedCategory = harness.Grocery;
        harness.Busy = true;
        Assert.IsFalse(row.SaveCommand.CanExecute(null), "busy");
        Assert.IsFalse(harness.ViewModel.RefreshCommand.CanExecute(null), "refresh busy");
    }

    [TestMethod]
    public async Task Row_DefaultsToSuggestedNoneCategoryAndFormatsDateAndAmount()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(1);
        var row = harness.ViewModel.Rows.Single();

        Assert.AreEqual(harness.None, row.SelectedCategory);
        Assert.AreEqual("2026-01-30", row.BookingDateText);
        Assert.AreEqual("-2.99", row.AmountText);
        Assert.IsFalse(row.IsModified);
    }

    [TestMethod]
    public async Task RefreshAsync_ParseErrors_ClearRowsAndResetSummaryAndPage()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(TwoPageRowCount);

        await harness.ViewModel.RefreshAsync("not|a|valid|row");

        AssertEmptyList(harness.ViewModel);
    }

    [TestMethod]
    public async Task RefreshAsync_MissingFile_ClearsRows()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(3);
        harness.FilePath = Path.Combine(Path.GetTempPath(), $"homecharts-missing-{Guid.NewGuid():N}.txt");

        await harness.ViewModel.RefreshAsync();

        AssertEmptyList(harness.ViewModel);
    }

    [TestMethod]
    public async Task RefreshAsync_EmptyFile_ClearsRows()
    {
        using var harness = await PotentialRulesHarness.CreateAsync(3);
        await File.WriteAllTextAsync(harness.FilePath, string.Empty);

        await harness.ViewModel.RefreshAsync();

        AssertEmptyList(harness.ViewModel);
        Assert.IsFalse(harness.ViewModel.ApplyAllCommand.CanExecute(null), "cleared rows must also leave the Apply All set");
    }

    [TestMethod]
    public async Task MainWindowViewModel_ImportOfMoreThanOnePage_PopulatesPotentialRules()
    {
        var repositories = new TestRepositories();
        var viewModel = TestViewModelFactory.Create(repositories);
        await viewModel.LoadInitialStateAsync();
        var filePath = Path.Combine(Path.GetTempPath(), $"homecharts-potential-rules-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(filePath, PotentialRulesHarness.BuildContent(130));

        try
        {
            await viewModel.ImportFromSelectedFileAsync(filePath);

            Assert.IsFalse(viewModel.Status.StartsWith("Error", StringComparison.Ordinal), $"unexpected failure: {viewModel.Status}");
            Assert.AreEqual("Potential new rules: 130", viewModel.PotentialRules.Summary);
            Assert.AreEqual("Page 1/2", viewModel.PotentialRules.PageInfo);
            Assert.AreEqual(PotentialRulesViewModel.PageSize, viewModel.PotentialRules.Rows.Count);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static void AssertEmptyList(PotentialRulesViewModel viewModel)
    {
        Assert.AreEqual("Potential new rules: 0", viewModel.Summary);
        Assert.AreEqual("Page 1/1", viewModel.PageInfo);
        Assert.AreEqual(0, viewModel.Rows.Count);
    }
}

/// <summary>Builds a <see cref="PotentialRulesViewModel"/> over in-memory repositories and a temp statement file.</summary>
internal sealed class PotentialRulesHarness : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private int _busyCounter;

    private PotentialRulesHarness()
    {
        Repositories = new TestRepositories();
    }

    public TestRepositories Repositories { get; }
    public PotentialRulesViewModel ViewModel { get; private set; } = null!;
    public CategoryOptionViewModel None { get; private set; } = null!;
    public CategoryOptionViewModel Grocery { get; private set; } = null!;
    public CategoryOptionViewModel Car { get; private set; } = null!;
    public string Content { get; private set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public bool Busy { get; set; }
    public Task LastRun { get; private set; } = Task.CompletedTask;
    public List<string> Statuses { get; } = [];
    public int DependentsRefreshCount { get; private set; }

    public static async Task<PotentialRulesHarness> CreateAsync(int rowCount)
    {
        var harness = new PotentialRulesHarness();
        var categories = new List<CategoryOptionViewModel>();
        foreach (var name in new[] { "None", "Grocery", "Car" })
        {
            var category = new Category { Name = name };
            await harness.Repositories.Categories.UpsertAsync(category);
            categories.Add(new CategoryOptionViewModel(category.Id, name, category.ColorHex));
        }

        harness.None = categories[0];
        harness.Grocery = categories[1];
        harness.Car = categories[2];

        var createRule = new HomeCharts.Application.UseCases.CreateCategorizationRuleUseCase(harness.Repositories.Rules);
        harness.ViewModel = new PotentialRulesViewModel(
            new HomeCharts.Application.UseCases.PreviewPotentialRulesUseCase(
                harness.Repositories.Rules,
                harness.Repositories.Categories,
                harness.Repositories.PrefixFilters,
                new HomeCharts.Application.Import.BankStatementParser()),
            new HomeCharts.Application.UseCases.ApplyPotentialRulesUseCase(createRule),
            categories,
            new PotentialRulesHost(
                () => harness.FilePath,
                () => harness.Busy,
                harness.RunBusyAsync,
                message => harness.Statuses.Add(message),
                () =>
                {
                    harness.DependentsRefreshCount++;
                    return Task.CompletedTask;
                }));

        harness.Content = BuildContent(rowCount);
        harness.FilePath = harness.NewTempFile(harness.Content);
        await harness.ViewModel.RefreshAsync(harness.Content);

        Assert.AreEqual($"Potential new rules: {rowCount}", harness.ViewModel.Summary);
        return harness;
    }

    /// <summary>Realistic BankRecipes-style rows (7 pipe-delimited columns), one unique merchant per row.</summary>
    public static string BuildContent(int rowCount)
    {
        var lines = new List<string>(rowCount);
        for (var i = 1; i <= rowCount; i++)
        {
            lines.Add(
                $"30/01/2026|COMPRA TARJ. 5402XXXXXXXX7020 TEST MERCHANT {i:0000}-CITY|02/02/2026|-{(1 + i % 50):0}.99|1000.00||5402__7020");
        }

        return string.Join('\n', lines);
    }

    /// <summary>Points the host at a different file with identical content (a "file change").</summary>
    public void SwitchToCopyOfFile() => FilePath = NewTempFile(Content);

    /// <summary>The unique tail of the row's baseline pattern, e.g. "TEST MERCHANT 0002-CITY".</summary>
    public string UniquePatternOf(PotentialRuleRowViewModel row)
    {
        var baseline = row.Draft.SuggestedPattern;
        var index = baseline.IndexOf("TEST MERCHANT", StringComparison.Ordinal);
        return index >= 0 ? baseline[index..] : baseline;
    }

    public static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (condition())
                {
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                // rows are momentarily rebuilt while a refresh is in flight
            }

            await Task.Delay(10);
        }

        Assert.Fail("condition was not met within the test timeout");
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            File.Delete(file);
        }
    }

    private Task RunBusyAsync(Func<Task> action)
    {
        LastRun = RunAsync(action);
        return LastRun;
    }

    private async Task RunAsync(Func<Task> action)
    {
        _busyCounter++;
        Busy = true;
        try
        {
            await action();
        }
        finally
        {
            Busy = --_busyCounter > 0;
        }
    }

    private string NewTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"homecharts-potential-rules-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }
}
