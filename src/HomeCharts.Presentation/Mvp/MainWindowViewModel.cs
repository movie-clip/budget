using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using HomeCharts.Application.Import;
using HomeCharts.Application.UseCases;

namespace HomeCharts.Presentation.Mvp;

public sealed class MainWindowViewModel : ObservableObject
{
    private const string DefaultCardPrefix = "COMPRA TARJ. 5402XXXXXXXX7020";
    private static readonly string[] ExpenseCategoryOrder =
    [
        "None",
        "Cafe",
        "Shopping",
        "Grocery",
        "Car",
        "Entertainment",
        "House",
        "Medicine",
        "Rent",
        "Utility",
        "Smoke",
        "Transport",
        "Services",
        "Income",
        "Transfer",
        "Others"
    ];
    private readonly InitializeDatabaseUseCase _initializeDatabaseUseCase;
    private readonly SeedDefaultCategoriesUseCase _seedDefaultCategoriesUseCase;
    private readonly ImportBankStatementUseCase _importBankStatementUseCase;
    private readonly PreviewMatchedTransactionsUseCase _previewMatchedTransactionsUseCase;
    private readonly MergeMatchedTransactionsUseCase _mergeMatchedTransactionsUseCase;
    private readonly ApplyCategorizationRulesUseCase _applyCategorizationRulesUseCase;
    private readonly PreviewPotentialRulesUseCase _previewPotentialRulesUseCase;
    private readonly PreviewParsedCategoryExpensesUseCase _previewParsedCategoryExpensesUseCase;
    private readonly DeleteAllRulesUseCase _deleteAllRulesUseCase;
    private readonly DeleteAllTransactionsUseCase _deleteAllTransactionsUseCase;
    private readonly CreateCategorizationRuleUseCase _createCategorizationRuleUseCase;
    private readonly GetLedgerEntriesUseCase _getLedgerEntriesUseCase;
    private readonly BuildDashboardSnapshotUseCase _buildDashboardSnapshotUseCase;
    private readonly GetCategoriesUseCase _getCategoriesUseCase;
    private readonly ManualRecategorizationUseCase _manualRecategorizationUseCase;
    private readonly GetPrefixFiltersUseCase _getPrefixFiltersUseCase;
    private readonly AddPrefixFilterUseCase _addPrefixFilterUseCase;
    private readonly RemovePrefixFilterUseCase _removePrefixFilterUseCase;
    private readonly CreateDatabaseBackupUseCase _createDatabaseBackupUseCase;
    private readonly RestoreDatabaseBackupUseCase _restoreDatabaseBackupUseCase;
    private readonly ExportDataPackageUseCase _exportDataPackageUseCase;
    private readonly ImportDataPackageUseCase _importDataPackageUseCase;

    private string _importFilePath = Path.Combine("BankRecipes", "June2025.txt");
    private string _backupFilePath = Path.Combine("artifacts", "backup", "homecharts-backup.db");
    private string _transferFilePath = Path.Combine("artifacts", "transfer", "homecharts-data.json");
    private string _searchText = string.Empty;
    private bool _onlyUncategorized;
    private LedgerDatePreset _selectedDatePreset = LedgerDatePreset.Last12Months;
    private DateOnly _customFromDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-1));
    private DateOnly _customToDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    private string _selectedSourceAccountFilter = string.Empty;
    private string _minAmountFilter = string.Empty;
    private string _maxAmountFilter = string.Empty;
    private CategoryOptionViewModel? _selectedFilterCategory;
    private DatePresetOptionViewModel? _selectedDatePresetOption;
    private string _status = "Ready";
    private string _lastImportSummary = "No import executed yet.";
    private string _lastImportDetails = "Provide a file path and click Import.";
    private string _lastImportWarningBreakdown = "Warnings: 0";
    private string _matchedTransactionsSummary = "Matched transactions: 0";
    private string _newPrefixFilter = "COMPRA TARJ. 5402XXXXXXXX7020";
    private string _rulesQueueSummary = "Potential new rules: 0";
    private string _rulesQueuePageInfo = "Page 1/1";
    private string _parsedCategoriesSummary = "Expenses: 0";
    private string _pageInfo = "Page 1";
    private string _dashboardSummary = "Dashboard not loaded";
    private string _trendRangeText = "Last 12 months";
    private string _totalTransactionsText = "0";
    private string _incomeText = "0.00";
    private string _expensesText = "0.00";
    private string _netText = "0.00";
    private string _savingsRateText = "0.00%";
    private string _uncategorizedCountText = "0";
    private double _incomeBarPercent;
    private double _expensesBarPercent;
    private string _selectedView = "Dashboard";
    private bool _isBusy;
    private int _currentPage = 1;
    private int _totalMatchedCount;
    private string? _stagedMatchedFileContent;
    private string? _stagedMatchedSourceName;
    private int _rulesQueueCurrentPage = 1;
    private int _rulesQueueTotalMatchedCount;
    private readonly List<PotentialRuleRow> _rulesQueueRows = [];
    private const int PageSize = 200;
    private const int RulesQueuePageSize = 120;
    private LedgerRowViewModel? _selectedLedgerItem;
    private CategoryOptionViewModel? _selectedCategory;
    private TransactionDetailViewModel? _selectedTransactionDetail;
    private Dictionary<Guid, LedgerEntry> _ledgerEntriesById = new();

    public MainWindowViewModel(
        InitializeDatabaseUseCase initializeDatabaseUseCase,
        SeedDefaultCategoriesUseCase seedDefaultCategoriesUseCase,
        ImportBankStatementUseCase importBankStatementUseCase,
        PreviewMatchedTransactionsUseCase previewMatchedTransactionsUseCase,
        MergeMatchedTransactionsUseCase mergeMatchedTransactionsUseCase,
        ApplyCategorizationRulesUseCase applyCategorizationRulesUseCase,
        PreviewPotentialRulesUseCase previewPotentialRulesUseCase,
        PreviewParsedCategoryExpensesUseCase previewParsedCategoryExpensesUseCase,
        DeleteAllRulesUseCase deleteAllRulesUseCase,
        DeleteAllTransactionsUseCase deleteAllTransactionsUseCase,
        CreateCategorizationRuleUseCase createCategorizationRuleUseCase,
        GetLedgerEntriesUseCase getLedgerEntriesUseCase,
        BuildDashboardSnapshotUseCase buildDashboardSnapshotUseCase,
        GetCategoriesUseCase getCategoriesUseCase,
        ManualRecategorizationUseCase manualRecategorizationUseCase,
        GetPrefixFiltersUseCase getPrefixFiltersUseCase,
        AddPrefixFilterUseCase addPrefixFilterUseCase,
        RemovePrefixFilterUseCase removePrefixFilterUseCase,
        CreateDatabaseBackupUseCase createDatabaseBackupUseCase,
        RestoreDatabaseBackupUseCase restoreDatabaseBackupUseCase,
        ExportDataPackageUseCase exportDataPackageUseCase,
        ImportDataPackageUseCase importDataPackageUseCase)
    {
        _initializeDatabaseUseCase = initializeDatabaseUseCase;
        _seedDefaultCategoriesUseCase = seedDefaultCategoriesUseCase;
        _importBankStatementUseCase = importBankStatementUseCase;
        _previewMatchedTransactionsUseCase = previewMatchedTransactionsUseCase;
        _mergeMatchedTransactionsUseCase = mergeMatchedTransactionsUseCase;
        _applyCategorizationRulesUseCase = applyCategorizationRulesUseCase;
        _previewPotentialRulesUseCase = previewPotentialRulesUseCase;
        _previewParsedCategoryExpensesUseCase = previewParsedCategoryExpensesUseCase;
        _deleteAllRulesUseCase = deleteAllRulesUseCase;
        _deleteAllTransactionsUseCase = deleteAllTransactionsUseCase;
        _createCategorizationRuleUseCase = createCategorizationRuleUseCase;
        _getLedgerEntriesUseCase = getLedgerEntriesUseCase;
        _buildDashboardSnapshotUseCase = buildDashboardSnapshotUseCase;
        _getCategoriesUseCase = getCategoriesUseCase;
        _manualRecategorizationUseCase = manualRecategorizationUseCase;
        _getPrefixFiltersUseCase = getPrefixFiltersUseCase;
        _addPrefixFilterUseCase = addPrefixFilterUseCase;
        _removePrefixFilterUseCase = removePrefixFilterUseCase;
        _createDatabaseBackupUseCase = createDatabaseBackupUseCase;
        _restoreDatabaseBackupUseCase = restoreDatabaseBackupUseCase;
        _exportDataPackageUseCase = exportDataPackageUseCase;
        _importDataPackageUseCase = importDataPackageUseCase;

        InitializeCommand = new DelegateCommand(() => _ = InitializeAsync(), () => !IsBusy);
        ImportCommand = new DelegateCommand(() => _ = ImportAsync(), () => !IsBusy);
        ApplyRulesCommand = new DelegateCommand(() => _ = ApplyRulesAsync(), () => !IsBusy);
        RefreshCommand = new DelegateCommand(() => _ = RefreshAsync(), () => !IsBusy);
        BackupCommand = new DelegateCommand(() => _ = BackupAsync(), () => !IsBusy);
        RestoreCommand = new DelegateCommand(() => _ = RestoreAsync(), () => !IsBusy);
        ExportCommand = new DelegateCommand(() => _ = ExportAsync(), () => !IsBusy);
        ImportPackageCommand = new DelegateCommand(() => _ = ImportPackageAsync(), () => !IsBusy);
        ApplyFiltersCommand = new DelegateCommand(() => _ = ApplyFiltersAsync(), () => !IsBusy);
        RefreshRulesQueueCommand = new DelegateCommand(() => _ = RefreshRulesQueueAsync(), () => !IsBusy);
        NextRulesQueuePageCommand = new DelegateCommand(() => MoveNextRulesQueuePage(), () => !IsBusy && CanMoveNextRulesQueuePage());
        PreviousRulesQueuePageCommand = new DelegateCommand(() => MovePreviousRulesQueuePage(), () => !IsBusy && _rulesQueueCurrentPage > 1);
        ClearAllRulesCommand = new DelegateCommand(() => _ = ClearAllRulesAsync(), () => !IsBusy);
        ClearAllTransactionsCommand = new DelegateCommand(() => _ = ClearAllTransactionsAsync(), () => !IsBusy);
        MergeMatchedTransactionsCommand = new DelegateCommand(() => _ = MergeMatchedTransactionsAsync(), () => !IsBusy && CanMergeMatchedTransactions());
        SavePotentialRuleCommand = new DelegateCommand<EditableRulesImportedTransactionViewModel>(
            row => _ = SavePotentialRuleAsync(row),
            row => !IsBusy && row is not null);
        AddPrefixFilterCommand = new DelegateCommand(() => _ = AddPrefixFilterAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(NewPrefixFilter));
        RemovePrefixFilterCommand = new DelegateCommand<PrefixFilterItemViewModel>(
            item => _ = RemovePrefixFilterAsync(item),
            item => !IsBusy && item is not null);
        ResetTransactionFiltersCommand = new DelegateCommand(() => _ = ResetTransactionFiltersAsync(), () => !IsBusy);
        NextPageCommand = new DelegateCommand(() => _ = NextPageAsync(), () => !IsBusy && CanMoveNextPage());
        PreviousPageCommand = new DelegateCommand(() => _ = PreviousPageAsync(), () => !IsBusy && _currentPage > 1);
        ManualRecategorizeCommand = new DelegateCommand(() => _ = ManualRecategorizeAsync(), () => !IsBusy && SelectedLedgerItem is not null && SelectedCategory is not null);
        ShowDashboardViewCommand = new DelegateCommand(() => SetSelectedView("Dashboard"));
        ShowImportFiltersViewCommand = new DelegateCommand(() => SetSelectedView("Import"));
        ShowLedgerViewCommand = new DelegateCommand(() => SetSelectedView("Ledger"));
        ShowUtilityViewCommand = new DelegateCommand(() => SetSelectedView("Utility"));

        _selectedDatePresetOption = DatePresetOptions.FirstOrDefault(option => option.Value == _selectedDatePreset)
            ?? DatePresetOptions.First();
    }

    public ObservableCollection<LedgerRowViewModel> LedgerItems { get; } = [];
    public ObservableCollection<CategoryOptionViewModel> Categories { get; } = [];
    public ObservableCollection<CategoryOptionViewModel> ExpenseCategories { get; } = [];
    public ObservableCollection<DashboardTrendRowViewModel> TrendRows { get; } = [];
    public ObservableCollection<DashboardTrendBarViewModel> TrendChartBars { get; } = [];
    public ObservableCollection<DashboardCategoryRowViewModel> CategoryRows { get; } = [];
    public ObservableCollection<DashboardExpenseCategoryBarViewModel> ExpenseCategoryBars { get; } = [];
    public ObservableCollection<DashboardUncategorizedRowViewModel> UncategorizedRows { get; } = [];
    public ObservableCollection<EditableRulesImportedTransactionViewModel> ImportedRuleTransactions { get; } = [];
    public ObservableCollection<MatchedImportedTransactionViewModel> MatchedImportTransactions { get; } = [];
    public ObservableCollection<ParsedCategoryExpenseViewModel> ParsedCategoryExpenses { get; } = [];
    public ObservableCollection<PrefixFilterItemViewModel> PrefixFilters { get; } = [];
    public ObservableCollection<string> SourceAccountFilterOptions { get; } = [];
    public ObservableCollection<DatePresetOptionViewModel> DatePresetOptions { get; } =
    [
        new DatePresetOptionViewModel(LedgerDatePreset.Last30Days, "Last 30 days"),
        new DatePresetOptionViewModel(LedgerDatePreset.Last90Days, "Last 90 days"),
        new DatePresetOptionViewModel(LedgerDatePreset.ThisMonth, "This month"),
        new DatePresetOptionViewModel(LedgerDatePreset.ThisYear, "This year"),
        new DatePresetOptionViewModel(LedgerDatePreset.Last12Months, "Last 12 months"),
        new DatePresetOptionViewModel(LedgerDatePreset.Last5Years, "Last 5 years"),
        new DatePresetOptionViewModel(LedgerDatePreset.Custom, "Custom")
    ];

    public ICommand InitializeCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ApplyRulesCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand BackupCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportPackageCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public ICommand RefreshRulesQueueCommand { get; }
    public ICommand NextRulesQueuePageCommand { get; }
    public ICommand PreviousRulesQueuePageCommand { get; }
    public ICommand ClearAllRulesCommand { get; }
    public ICommand ClearAllTransactionsCommand { get; }
    public ICommand MergeMatchedTransactionsCommand { get; }
    public ICommand SavePotentialRuleCommand { get; }
    public ICommand AddPrefixFilterCommand { get; }
    public ICommand RemovePrefixFilterCommand { get; }
    public ICommand ResetTransactionFiltersCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand ManualRecategorizeCommand { get; }
    public ICommand ShowDashboardViewCommand { get; }
    public ICommand ShowImportFiltersViewCommand { get; }
    public ICommand ShowLedgerViewCommand { get; }
    public ICommand ShowUtilityViewCommand { get; }

    public bool IsDashboardViewSelected => _selectedView == "Dashboard";
    public bool IsImportFiltersViewSelected => _selectedView == "Import";
    public bool IsLedgerViewSelected => _selectedView == "Ledger";
    public bool IsUtilityViewSelected => _selectedView == "Utility";

    public string ImportFilePath
    {
        get => _importFilePath;
        set => SetProperty(ref _importFilePath, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string LastImportSummary
    {
        get => _lastImportSummary;
        private set => SetProperty(ref _lastImportSummary, value);
    }

    public string LastImportDetails
    {
        get => _lastImportDetails;
        private set => SetProperty(ref _lastImportDetails, value);
    }

    public string LastImportWarningBreakdown
    {
        get => _lastImportWarningBreakdown;
        private set => SetProperty(ref _lastImportWarningBreakdown, value);
    }

    public string MatchedTransactionsSummary
    {
        get => _matchedTransactionsSummary;
        private set => SetProperty(ref _matchedTransactionsSummary, value);
    }

    public string NewPrefixFilter
    {
        get => _newPrefixFilter;
        set
        {
            if (!SetProperty(ref _newPrefixFilter, value))
            {
                return;
            }

            RaiseCommands();
        }
    }

    public string RulesQueueSummary
    {
        get => _rulesQueueSummary;
        private set => SetProperty(ref _rulesQueueSummary, value);
    }

    public string RulesQueuePageInfo
    {
        get => _rulesQueuePageInfo;
        private set => SetProperty(ref _rulesQueuePageInfo, value);
    }

    public string ParsedCategoriesSummary
    {
        get => _parsedCategoriesSummary;
        private set => SetProperty(ref _parsedCategoriesSummary, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public bool OnlyUncategorized
    {
        get => _onlyUncategorized;
        set => SetProperty(ref _onlyUncategorized, value);
    }

    public LedgerDatePreset SelectedDatePreset
    {
        get => _selectedDatePreset;
        set
        {
            if (!SetProperty(ref _selectedDatePreset, value))
            {
                return;
            }

            var option = DatePresetOptions.FirstOrDefault(item => item.Value == value);
            if (option is not null && !ReferenceEquals(_selectedDatePresetOption, option))
            {
                _selectedDatePresetOption = option;
                OnPropertyChanged(nameof(SelectedDatePresetOption));
            }
        }
    }

    public DatePresetOptionViewModel? SelectedDatePresetOption
    {
        get => _selectedDatePresetOption;
        set
        {
            if (!SetProperty(ref _selectedDatePresetOption, value))
            {
                return;
            }

            if (value is not null && value.Value != _selectedDatePreset)
            {
                _selectedDatePreset = value.Value;
                OnPropertyChanged(nameof(SelectedDatePreset));
            }
        }
    }

    public DateOnly CustomFromDate
    {
        get => _customFromDate;
        set => SetProperty(ref _customFromDate, value);
    }

    public DateOnly CustomToDate
    {
        get => _customToDate;
        set => SetProperty(ref _customToDate, value);
    }

    public string SelectedSourceAccountFilter
    {
        get => _selectedSourceAccountFilter;
        set => SetProperty(ref _selectedSourceAccountFilter, value);
    }

    public CategoryOptionViewModel? SelectedFilterCategory
    {
        get => _selectedFilterCategory;
        set => SetProperty(ref _selectedFilterCategory, value);
    }

    public string MinAmountFilter
    {
        get => _minAmountFilter;
        set => SetProperty(ref _minAmountFilter, value);
    }

    public string MaxAmountFilter
    {
        get => _maxAmountFilter;
        set => SetProperty(ref _maxAmountFilter, value);
    }

    public string PageInfo
    {
        get => _pageInfo;
        private set => SetProperty(ref _pageInfo, value);
    }

    public string DashboardSummary
    {
        get => _dashboardSummary;
        private set => SetProperty(ref _dashboardSummary, value);
    }

    public string TrendRangeText
    {
        get => _trendRangeText;
        private set => SetProperty(ref _trendRangeText, value);
    }

    public string TotalTransactionsText
    {
        get => _totalTransactionsText;
        private set => SetProperty(ref _totalTransactionsText, value);
    }

    public string IncomeText
    {
        get => _incomeText;
        private set => SetProperty(ref _incomeText, value);
    }

    public string ExpensesText
    {
        get => _expensesText;
        private set => SetProperty(ref _expensesText, value);
    }

    public string NetText
    {
        get => _netText;
        private set => SetProperty(ref _netText, value);
    }

    public string SavingsRateText
    {
        get => _savingsRateText;
        private set => SetProperty(ref _savingsRateText, value);
    }

    public string UncategorizedCountText
    {
        get => _uncategorizedCountText;
        private set => SetProperty(ref _uncategorizedCountText, value);
    }

    public double IncomeBarPercent
    {
        get => _incomeBarPercent;
        private set => SetProperty(ref _incomeBarPercent, value);
    }

    public double ExpensesBarPercent
    {
        get => _expensesBarPercent;
        private set => SetProperty(ref _expensesBarPercent, value);
    }

    public string BackupFilePath
    {
        get => _backupFilePath;
        set => SetProperty(ref _backupFilePath, value);
    }

    public string TransferFilePath
    {
        get => _transferFilePath;
        set => SetProperty(ref _transferFilePath, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
            {
                return;
            }

            RaiseCommands();
        }
    }

    public LedgerRowViewModel? SelectedLedgerItem
    {
        get => _selectedLedgerItem;
        set
        {
            if (!SetProperty(ref _selectedLedgerItem, value))
            {
                return;
            }

            UpdateSelectedTransactionDetail();
            RaiseCommands();
        }
    }

    public TransactionDetailViewModel? SelectedTransactionDetail
    {
        get => _selectedTransactionDetail;
        private set => SetProperty(ref _selectedTransactionDetail, value);
    }

    public CategoryOptionViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (!SetProperty(ref _selectedCategory, value))
            {
                return;
            }

            RaiseCommands();
        }
    }

    public async Task InitializeAsync()
    {
        await RunBusyAsync(async () =>
        {
            await _initializeDatabaseUseCase.ExecuteAsync();
            await _seedDefaultCategoriesUseCase.ExecuteAsync();
            await EnsureDefaultPrefixFiltersAsync();
            await ReloadCategoriesAsync();
            await ReloadPrefixFiltersAsync();
            await RefreshLedgerAsync();
            await RefreshDashboardAsync();
            await RefreshRulesQueueAsync();
            await RefreshParsedCategoryExpensesAsync();
            Status = "Initialized DB.";
        });
    }

    public async Task ImportFromSelectedFileAsync(string filePath)
    {
        ImportFilePath = filePath;
        await ImportAsync();
    }

    public async Task PreviewMatchedTransactionsFromSelectedFileAsync(string filePath)
    {
        ImportFilePath = filePath;
        await PreviewMatchedTransactionsAsync();
    }

    private async Task ImportAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!File.Exists(ImportFilePath))
            {
                Status = $"File not found: {ImportFilePath}";
                LastImportSummary = "Import failed before parsing.";
                LastImportDetails = "Selected file path does not exist.";
                LastImportWarningBreakdown = "Warnings: 0";
                return;
            }

            var content = await File.ReadAllTextAsync(ImportFilePath);
            if (string.IsNullOrWhiteSpace(content))
            {
                Status = "Import failed: file is empty.";
                LastImportSummary = "Import failed: empty file content.";
                LastImportDetails = "No rows detected in file.";
                LastImportWarningBreakdown = "Warnings: 0";
                return;
            }

            var sourceName = Path.GetFileNameWithoutExtension(ImportFilePath);
            var result = await _importBankStatementUseCase.ExecuteAsync(sourceName, content);
            var warningBreakdown = BuildWarningBreakdown(result.Warnings);
            LastImportWarningBreakdown = warningBreakdown;

            if (result.IsDuplicate)
            {
                Status = "Import skipped: duplicate file hash.";
                LastImportSummary = $"Duplicate import skipped for {result.SourceName}.";
                LastImportDetails = $"File hash: {result.FileHash}";
                await RefreshLedgerAsync();
                await RefreshRulesQueueAsync(content);
                await RefreshParsedCategoryExpensesAsync(content);
                return;
            }

            if (result.Errors.Count > 0)
            {
                Status = $"Import failed. First error line {result.Errors[0].LineNumber}: {result.Errors[0].Message}";
                LastImportSummary = $"Import failed with {result.Errors.Count} parse error(s).";
                LastImportDetails = $"Parsed rows: {result.Metrics.ParsedRowCount}/{result.Metrics.NonEmptyLineCount}. First error line {result.Errors[0].LineNumber}.";
                return;
            }

            await RefreshLedgerAsync();
            var warningSuffix = result.Warnings.Count > 0
                ? $" Warnings: {result.Warnings.Count} (first: {result.Warnings[0].Message})"
                : string.Empty;

            var range = GetDefaultRange();
            var applyResult = await _applyCategorizationRulesUseCase.ExecuteAsync(range.From, range.To);

            await RefreshDashboardAsync();
            await RefreshRulesQueueAsync(content);
            await RefreshParsedCategoryExpensesAsync(content);
            Status = $"Imported {result.ImportedCount}/{result.Metrics.ParsedRowCount} rows from {sourceName}. Skipped duplicates: {result.SkippedDuplicateRowCount}.{warningSuffix} Auto-categorized: {applyResult.CategorizedCount}.";
            LastImportSummary = $"Imported {result.ImportedCount}/{result.Metrics.ParsedRowCount} rows from {result.SourceName}.";
            LastImportDetails =
                $"Non-empty lines: {result.Metrics.NonEmptyLineCount}, Parsed: {result.Metrics.ParsedRowCount}, " +
                $"Skipped duplicates: {result.SkippedDuplicateRowCount}, Database duplicates: {result.Metrics.DuplicateExistingRowCount}.";
        });
    }

    private async Task PreviewMatchedTransactionsAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!File.Exists(ImportFilePath))
            {
                MatchedImportTransactions.Clear();
                _stagedMatchedFileContent = null;
                _stagedMatchedSourceName = null;
                MatchedTransactionsSummary = "Matched transactions: 0";
                Status = $"File not found: {ImportFilePath}";
                RaiseCommands();
                return;
            }

            var content = await File.ReadAllTextAsync(ImportFilePath);
            if (string.IsNullOrWhiteSpace(content))
            {
                MatchedImportTransactions.Clear();
                _stagedMatchedFileContent = null;
                _stagedMatchedSourceName = null;
                MatchedTransactionsSummary = "Matched transactions: 0";
                Status = "Parse failed: file is empty.";
                RaiseCommands();
                return;
            }

            var preview = await _previewMatchedTransactionsUseCase.ExecuteAsync(content);
            if (preview.Errors.Count > 0)
            {
                MatchedImportTransactions.Clear();
                _stagedMatchedFileContent = null;
                _stagedMatchedSourceName = null;
                MatchedTransactionsSummary = "Matched transactions: 0";
                Status = $"Parse failed. First error line {preview.Errors[0].LineNumber}: {preview.Errors[0].Message}";
                RaiseCommands();
                return;
            }

            MatchedImportTransactions.Clear();
            foreach (var row in preview.MatchedRows)
            {
                MatchedImportTransactions.Add(new MatchedImportedTransactionViewModel(
                    row.LineNumber,
                    row.BookingDate,
                    FormatDescriptionForDisplay(row.Description),
                    row.Amount,
                    row.SourceAccount ?? string.Empty,
                    row.CategoryName,
                    row.RuleName));
            }

            _stagedMatchedFileContent = content;
            _stagedMatchedSourceName = Path.GetFileNameWithoutExtension(ImportFilePath);
            MatchedTransactionsSummary =
                $"Matched {preview.MatchedRows.Count}/{preview.Metrics.ParsedRowCount} • Unmatched {preview.UnmatchedCount} • Warnings {preview.Warnings.Count}";
            Status = $"Preview ready. Matched {preview.MatchedRows.Count} transactions with rules.";
            RaiseCommands();
        });
    }

    private async Task MergeMatchedTransactionsAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(_stagedMatchedFileContent) || string.IsNullOrWhiteSpace(_stagedMatchedSourceName))
            {
                Status = "No preview loaded. Browse a file in Transactions first.";
                return;
            }

            var result = await _mergeMatchedTransactionsUseCase.ExecuteAsync(_stagedMatchedSourceName, _stagedMatchedFileContent);
            if (result.IsDuplicate)
            {
                await RefreshLedgerAsync();
                await RefreshDashboardAsync();
                Status =
                    $"Merge skipped for {result.SourceName}: duplicate file hash. " +
                    $"Budget transactions in DB range: {TotalTransactionsText}.";
                SetSelectedView("Dashboard");
                return;
            }

            if (result.Errors.Count > 0)
            {
                Status = $"Merge failed. First error line {result.Errors[0].LineNumber}: {result.Errors[0].Message}";
                return;
            }

            _currentPage = 1;
            await RefreshLedgerAsync();
            await RefreshDashboardAsync();
            await RefreshRulesQueueAsync(_stagedMatchedFileContent);
            await RefreshParsedCategoryExpensesAsync(_stagedMatchedFileContent);

            MatchedImportTransactions.Clear();
            _stagedMatchedFileContent = null;
            _stagedMatchedSourceName = null;
            MatchedTransactionsSummary = "Matched transactions: 0";

            Status =
                $"Merged {result.Metrics.ImportedRowCount}/{result.MatchedCount} matched rows from {result.SourceName} into budget. " +
                $"Unmatched: {result.UnmatchedCount}, Skipped duplicates: {result.SkippedDuplicateRowCount}, " +
                $"Budget transactions in DB range: {TotalTransactionsText}.";
            SetSelectedView("Dashboard");
            RaiseCommands();
        });
    }

    private async Task ApplyRulesAsync()
    {
        await RunBusyAsync(async () =>
        {
            var range = GetDefaultRange();
            var result = await _applyCategorizationRulesUseCase.ExecuteAsync(range.From, range.To);
            await RefreshLedgerAsync();
            await RefreshDashboardAsync();
            await RefreshRulesQueueAsync();
            await RefreshParsedCategoryExpensesAsync();
            Status = $"Rules applied. Categorized: {result.CategorizedCount}, Manual skips: {result.SkippedByManualOverrideCount}, No match: {result.NoMatchCount}.";
        });
    }

    private async Task RefreshAsync()
    {
        await RunBusyAsync(async () =>
        {
            await _seedDefaultCategoriesUseCase.ExecuteAsync();
            await EnsureDefaultPrefixFiltersAsync();
            await ReloadCategoriesAsync();
            await ReloadPrefixFiltersAsync();
            _currentPage = 1;
            await RefreshLedgerAsync();
            await RefreshDashboardAsync();
            await RefreshRulesQueueAsync();
            await RefreshParsedCategoryExpensesAsync();
            Status = $"Ledger refreshed. Items: {LedgerItems.Count}.";
        });
    }

    private async Task ApplyFiltersAsync()
    {
        await RunBusyAsync(async () =>
        {
            _currentPage = 1;
            await RefreshLedgerAsync();
            Status = $"Filters applied. Matched {_totalMatchedCount} rows.";
        });
    }

    private async Task ClearAllRulesAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _deleteAllRulesUseCase.ExecuteAsync();
            await RefreshRulesQueueAsync();
            await RefreshParsedCategoryExpensesAsync();
            Status = $"All rules deleted. Removed: {result.DeletedCount}.";
        });
    }

    private async Task ClearAllTransactionsAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _deleteAllTransactionsUseCase.ExecuteAsync();
            MatchedImportTransactions.Clear();
            _stagedMatchedFileContent = null;
            _stagedMatchedSourceName = null;
            MatchedTransactionsSummary = "Matched transactions: 0";
            _currentPage = 1;
            await RefreshLedgerAsync();
            await RefreshDashboardAsync();
            Status = $"All transactions deleted. Removed tx: {result.DeletedTransactions}, overrides: {result.DeletedOverrides}, imports: {result.DeletedImportBatches}.";
        });
    }

    private async Task ResetTransactionFiltersAsync()
    {
        await RunBusyAsync(async () =>
        {
            SearchText = string.Empty;
            OnlyUncategorized = false;
            SelectedDatePreset = LedgerDatePreset.Last12Months;
            CustomFromDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-1));
            CustomToDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            SelectedSourceAccountFilter = string.Empty;
            SelectedFilterCategory = null;
            MinAmountFilter = string.Empty;
            MaxAmountFilter = string.Empty;

            _currentPage = 1;
            await RefreshLedgerAsync();
            Status = "Transaction filters reset.";
        });
    }

    private async Task SavePotentialRuleAsync(EditableRulesImportedTransactionViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            if (row.SelectedCategory is null)
            {
                Status = "Select a category before saving a rule.";
                return;
            }

            var result = await _createCategorizationRuleUseCase.ExecuteAsync(row.Description, row.SelectedCategory.Id);
            if (result.IsInvalidInput)
            {
                Status = "Description is empty. Enter a matching string first.";
                return;
            }

            if (result.IsCreated)
            {
                await RefreshRulesQueueAsync();
                await RefreshParsedCategoryExpensesAsync();
                Status = $"Rule saved for '{row.Description.Trim()}'.";
                return;
            }

            Status = "Matching rule already exists.";
        });
    }

    private async Task AddPrefixFilterAsync()
    {
        await RunBusyAsync(async () =>
        {
            var isAdded = await _addPrefixFilterUseCase.ExecuteAsync(NewPrefixFilter);
            if (!isAdded)
            {
                Status = "Prefix cannot be empty.";
                return;
            }

            await ReloadPrefixFiltersAsync();
            await RefreshLedgerAsync();
            await RefreshRulesQueueAsync();
            Status = "Prefix filter saved.";
        });
    }

    private async Task RemovePrefixFilterAsync(PrefixFilterItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            var removed = await _removePrefixFilterUseCase.ExecuteAsync(item.Value);
            if (!removed)
            {
                Status = "Prefix could not be removed.";
                return;
            }

            await ReloadPrefixFiltersAsync();
            await RefreshLedgerAsync();
            await RefreshRulesQueueAsync();
            Status = "Prefix filter removed.";
        });
    }

    private async Task BackupAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _createDatabaseBackupUseCase.ExecuteAsync(BackupFilePath);
            Status = $"Backup created: {result.BackupFilePath}";
        });
    }

    private async Task RestoreAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _restoreDatabaseBackupUseCase.ExecuteAsync(BackupFilePath);
            await ReloadPrefixFiltersAsync();
            await RefreshLedgerAsync();
            await RefreshDashboardAsync();
            await RefreshRulesQueueAsync();
            await RefreshParsedCategoryExpensesAsync();
            Status = $"Database restored from: {result.SourceBackupPath}";
        });
    }

    private async Task ExportAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _exportDataPackageUseCase.ExecuteAsync(TransferFilePath);
            Status = $"Export complete: tx={result.TransactionCount}, categories={result.CategoryCount}, rules={result.RuleCount}";
        });
    }

    private async Task ImportPackageAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await _importDataPackageUseCase.ExecuteAsync(TransferFilePath);
            await ReloadCategoriesAsync();
            await ReloadPrefixFiltersAsync();
            await RefreshLedgerAsync();
            await RefreshDashboardAsync();
            await RefreshRulesQueueAsync();
            await RefreshParsedCategoryExpensesAsync();
            Status = $"Import complete: tx={result.ImportedTransactions}, categories={result.ImportedCategories}, rules={result.ImportedRules}";
        });
    }

    private async Task NextPageAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (!CanMoveNextPage())
            {
                return;
            }

            _currentPage++;
            await RefreshLedgerAsync();
        });
    }

    private async Task PreviousPageAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (_currentPage <= 1)
            {
                return;
            }

            _currentPage--;
            await RefreshLedgerAsync();
        });
    }

    private async Task ManualRecategorizeAsync()
    {
        if (SelectedLedgerItem is null || SelectedCategory is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            var result = await _manualRecategorizationUseCase.ExecuteAsync(
                [SelectedLedgerItem.Id],
                SelectedCategory.Id,
                "Manual change from MVP UI");

            await RefreshLedgerAsync();
            await RefreshDashboardAsync();
            Status = $"Manual recategorization complete. Changed: {result.ChangedCount}.";
        });
    }

    private async Task ReloadCategoriesAsync()
    {
        var categories = await _getCategoriesUseCase.ExecuteAsync();

        Categories.Clear();
        foreach (var category in categories)
        {
            Categories.Add(new CategoryOptionViewModel(category.Id, category.Name, category.ColorHex));
        }

        ExpenseCategories.Clear();
        foreach (var categoryName in ExpenseCategoryOrder)
        {
            var match = Categories.FirstOrDefault(category =>
                string.Equals(category.Name, categoryName, StringComparison.OrdinalIgnoreCase));

            if (match is not null && ExpenseCategories.All(existing => !string.Equals(existing.Name, match.Name, StringComparison.OrdinalIgnoreCase)))
            {
                ExpenseCategories.Add(match);
            }
        }

        if (SelectedCategory is null || Categories.All(category => category.Id != SelectedCategory.Id))
        {
            SelectedCategory = ExpenseCategories.FirstOrDefault() ?? Categories.FirstOrDefault();
        }

        if (SelectedFilterCategory is not null && Categories.All(category => category.Id != SelectedFilterCategory.Id))
        {
            SelectedFilterCategory = null;
        }
    }

    private async Task ReloadPrefixFiltersAsync()
    {
        var filters = await _getPrefixFiltersUseCase.ExecuteAsync();

        PrefixFilters.Clear();
        foreach (var filter in filters)
        {
            PrefixFilters.Add(new PrefixFilterItemViewModel(filter));
        }
    }

    private async Task EnsureDefaultPrefixFiltersAsync()
    {
        await _addPrefixFilterUseCase.ExecuteAsync(DefaultCardPrefix);
    }

    private async Task RefreshLedgerAsync()
    {
        var range = ResolveLedgerDateRange();
        var minAmount = ParseNullableDecimal(MinAmountFilter);
        var maxAmount = ParseNullableDecimal(MaxAmountFilter);
        var query = new LedgerQuery(
            range.From,
            range.To,
            SearchText,
            OnlyUncategorized,
            (_currentPage - 1) * PageSize,
            PageSize,
            string.IsNullOrWhiteSpace(SelectedSourceAccountFilter) ? null : SelectedSourceAccountFilter,
            SelectedFilterCategory?.Id,
            minAmount,
            maxAmount,
            SelectedDatePreset);

        var result = await _getLedgerEntriesUseCase.ExecuteAsync(query);
        _ledgerEntriesById = result.Items.ToDictionary(static item => item.Id);

        LedgerItems.Clear();
        foreach (var item in result.Items)
        {
            LedgerItems.Add(new LedgerRowViewModel(
                item.Id,
                item.BookingDate,
                FormatDescriptionForDisplay(item.Description),
                item.Amount,
                item.CategoryName ?? "(Uncategorized)",
                item.SourceAccount ?? string.Empty));
        }

        var previousSourceAccount = SelectedSourceAccountFilter;
        SourceAccountFilterOptions.Clear();
        SourceAccountFilterOptions.Add(string.Empty);
        foreach (var sourceAccount in result.AvailableSourceAccounts)
        {
            SourceAccountFilterOptions.Add(sourceAccount);
        }

        if (!string.IsNullOrWhiteSpace(previousSourceAccount) &&
            !SourceAccountFilterOptions.Contains(previousSourceAccount, StringComparer.OrdinalIgnoreCase))
        {
            SelectedSourceAccountFilter = string.Empty;
        }

        _totalMatchedCount = result.TotalMatchedCount;
        var totalPages = Math.Max(1, (int)Math.Ceiling(_totalMatchedCount / (double)PageSize));
        if (_currentPage > totalPages)
        {
            _currentPage = totalPages;
        }

        PageInfo = $"Page {_currentPage}/{totalPages} • Matched {_totalMatchedCount} rows";
        UpdateSelectedTransactionDetail();
        RaiseCommands();
    }

    private async Task RefreshDashboardAsync()
    {
        var range = GetDefaultRange();
        var snapshot = await _buildDashboardSnapshotUseCase.ExecuteAsync(range.From, range.To, trendMonths: 12, uncategorizedLimit: 12);

        DashboardSummary =
            $"Tx: {snapshot.Kpi.TotalTransactions} • Income: {snapshot.Kpi.TotalIncome:0.00} • Expenses: {snapshot.Kpi.TotalExpenses:0.00} • Net: {snapshot.Kpi.NetAmount:0.00} • Savings: {snapshot.Kpi.SavingsRatePercent:0.00}% • Uncategorized: {snapshot.Kpi.UncategorizedCount}";

        TotalTransactionsText = snapshot.Kpi.TotalTransactions.ToString();
        IncomeText = snapshot.Kpi.TotalIncome.ToString("0.00");
        ExpensesText = snapshot.Kpi.TotalExpenses.ToString("0.00");
        NetText = snapshot.Kpi.NetAmount.ToString("0.00");
        SavingsRateText = snapshot.Kpi.SavingsRatePercent.ToString("0.00") + "%";
        UncategorizedCountText = snapshot.Kpi.UncategorizedCount.ToString();

        var totalFlow = Math.Abs(snapshot.Kpi.TotalIncome) + Math.Abs(snapshot.Kpi.TotalExpenses);
        if (totalFlow <= 0)
        {
            IncomeBarPercent = 0;
            ExpensesBarPercent = 0;
        }
        else
        {
            IncomeBarPercent = Math.Round((double)(Math.Abs(snapshot.Kpi.TotalIncome) / totalFlow * 100m), 2);
            ExpensesBarPercent = Math.Round((double)(Math.Abs(snapshot.Kpi.TotalExpenses) / totalFlow * 100m), 2);
        }

        TrendRows.Clear();
        TrendChartBars.Clear();

        if (snapshot.MonthlyTrend.Count > 0)
        {
            var fromMonth = snapshot.MonthlyTrend.First().Month;
            var toMonth = snapshot.MonthlyTrend.Last().Month;
            TrendRangeText = $"{fromMonth:MMM yyyy} - {toMonth:MMM yyyy}";
        }
        else
        {
            TrendRangeText = "No trend data";
        }

        var maxIncome = snapshot.MonthlyTrend.Any() ? snapshot.MonthlyTrend.Max(point => Math.Abs(point.Income)) : 0m;
        var maxExpenses = snapshot.MonthlyTrend.Any() ? snapshot.MonthlyTrend.Max(point => Math.Abs(point.Expenses)) : 0m;

        foreach (var point in snapshot.MonthlyTrend)
        {
            TrendRows.Add(new DashboardTrendRowViewModel(
                point.Month.ToString("yyyy-MM"),
                point.Income,
                point.Expenses,
                point.Net));

            var incomePercent = maxIncome <= 0m
                ? 0d
                : Math.Round((double)(Math.Abs(point.Income) / maxIncome * 100m), 2);

            var expensesPercent = maxExpenses <= 0m
                ? 0d
                : Math.Round((double)(Math.Abs(point.Expenses) / maxExpenses * 100m), 2);

            TrendChartBars.Add(new DashboardTrendBarViewModel(
                point.Month.ToString("yyyy-MM"),
                point.Income,
                point.Expenses,
                point.Net,
                incomePercent,
                expensesPercent));
        }

        CategoryRows.Clear();
        ExpenseCategoryBars.Clear();

        var maxCategoryAmount = snapshot.CategoryBreakdown.Any()
            ? snapshot.CategoryBreakdown.Max(item => item.Amount)
            : 0m;

        foreach (var category in snapshot.CategoryBreakdown)
        {
            CategoryRows.Add(new DashboardCategoryRowViewModel(
                category.CategoryName,
                category.Amount,
                category.TransactionCount));

            var percent = maxCategoryAmount <= 0m
                ? 0d
                : Math.Round((double)(category.Amount / maxCategoryAmount * 100m), 2);

            var transactionItems = category.Transactions
                .Select(transaction => new DashboardExpenseTransactionViewModel(
                    transaction.TransactionId,
                    transaction.BookingDate.ToString("yyyy-MM-dd"),
                    FormatDescriptionForDisplay(transaction.Description),
                    transaction.Amount))
                .ToArray();

            ExpenseCategoryBars.Add(new DashboardExpenseCategoryBarViewModel(
                category.CategoryName,
                category.Amount,
                category.TransactionCount,
                percent,
                transactionItems,
                OnExpenseCategoryExpanded));
        }

        UncategorizedRows.Clear();
        foreach (var item in snapshot.UncategorizedQueue)
        {
            UncategorizedRows.Add(new DashboardUncategorizedRowViewModel(
                item.TransactionId,
                item.BookingDate.ToString("yyyy-MM-dd"),
                FormatDescriptionForDisplay(item.Description),
                item.Amount));
        }
    }

    private void OnExpenseCategoryExpanded(DashboardExpenseCategoryBarViewModel expandedItem)
    {
        foreach (var item in ExpenseCategoryBars)
        {
            if (!ReferenceEquals(item, expandedItem) && item.IsExpanded)
            {
                item.IsExpanded = false;
            }
        }
    }

    private async Task RefreshRulesQueueAsync(string? fileContent = null)
    {
        ImportedRuleTransactions.Clear();

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            if (string.IsNullOrWhiteSpace(ImportFilePath) || !File.Exists(ImportFilePath))
            {
                RulesQueueSummary = "Potential new rules: 0";
                return;
            }

            fileContent = await File.ReadAllTextAsync(ImportFilePath);
        }

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            RulesQueueSummary = "Potential new rules: 0";
            return;
        }

        var preview = await _previewPotentialRulesUseCase.ExecuteAsync(fileContent);
        if (preview.Errors.Count > 0)
        {
            RulesQueueSummary = "Potential new rules: 0";
            RulesQueuePageInfo = "Page 1/1";
            _rulesQueueRows.Clear();
            _rulesQueueTotalMatchedCount = 0;
            _rulesQueueCurrentPage = 1;
            RaiseCommands();
            return;
        }

        _rulesQueueRows.Clear();
        _rulesQueueRows.AddRange(preview.PotentialRows);
        _rulesQueueTotalMatchedCount = _rulesQueueRows.Count;
        _rulesQueueCurrentPage = 1;
        RefreshRulesQueuePage();
        RulesQueueSummary = $"Potential new rules: {preview.PotentialRows.Count}";
    }

    private void MoveNextRulesQueuePage()
    {
        if (!CanMoveNextRulesQueuePage())
        {
            return;
        }

        _rulesQueueCurrentPage++;
        RefreshRulesQueuePage();
    }

    private void MovePreviousRulesQueuePage()
    {
        if (_rulesQueueCurrentPage <= 1)
        {
            return;
        }

        _rulesQueueCurrentPage--;
        RefreshRulesQueuePage();
    }

    private void RefreshRulesQueuePage()
    {
        ImportedRuleTransactions.Clear();

        if (_rulesQueueTotalMatchedCount <= 0)
        {
            _rulesQueueCurrentPage = 1;
            RulesQueuePageInfo = "Page 1/1";
            RaiseCommands();
            return;
        }

        var totalPages = Math.Max(1, (int)Math.Ceiling(_rulesQueueTotalMatchedCount / (double)RulesQueuePageSize));
        if (_rulesQueueCurrentPage > totalPages)
        {
            _rulesQueueCurrentPage = totalPages;
        }

        var skip = (_rulesQueueCurrentPage - 1) * RulesQueuePageSize;
        var pageRows = _rulesQueueRows
            .Skip(skip)
            .Take(RulesQueuePageSize);

        foreach (var row in pageRows)
        {
            ImportedRuleTransactions.Add(new EditableRulesImportedTransactionViewModel(
                Guid.NewGuid(),
                row.BookingDate,
                FormatDescriptionForDisplay(row.Description),
                row.Amount,
                GetDefaultPotentialRuleCategory()));
        }

        RulesQueuePageInfo = $"Page {_rulesQueueCurrentPage}/{totalPages}";
        RaiseCommands();
    }

    private async Task RefreshParsedCategoryExpensesAsync(string? fileContent = null)
    {
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            if (string.IsNullOrWhiteSpace(ImportFilePath) || !File.Exists(ImportFilePath))
            {
                ParsedCategoryExpenses.Clear();
                ParsedCategoriesSummary = "Expenses: 0";
                return;
            }

            fileContent = await File.ReadAllTextAsync(ImportFilePath);
        }

        ParsedCategoryExpenses.Clear();
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            ParsedCategoriesSummary = "Expenses: 0";
            return;
        }

        var preview = await _previewParsedCategoryExpensesUseCase.ExecuteAsync(fileContent);
        if (preview.Errors.Count > 0)
        {
            ParsedCategoriesSummary = "Expenses: 0";
            return;
        }

        foreach (var item in preview.Items)
        {
            ParsedCategoryExpenses.Add(new ParsedCategoryExpenseViewModel(
                item.Category,
                item.ExpenseAmount,
                item.Transactions));
        }

        ParsedCategoriesSummary = $"Expenses: {ParsedCategoryExpenses.Count}";
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception exception)
        {
            Status = $"Error: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static (DateOnly From, DateOnly To) GetDefaultRange()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return (today.AddYears(-5), today.AddDays(1));
    }

    private (DateOnly From, DateOnly To) ResolveLedgerDateRange()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return SelectedDatePreset switch
        {
            LedgerDatePreset.Last30Days => (today.AddDays(-30), today.AddDays(1)),
            LedgerDatePreset.Last90Days => (today.AddDays(-90), today.AddDays(1)),
            LedgerDatePreset.ThisMonth => (new DateOnly(today.Year, today.Month, 1), today.AddDays(1)),
            LedgerDatePreset.ThisYear => (new DateOnly(today.Year, 1, 1), today.AddDays(1)),
            LedgerDatePreset.Last12Months => (today.AddYears(-1), today.AddDays(1)),
            LedgerDatePreset.Last5Years => (today.AddYears(-5), today.AddDays(1)),
            _ => (CustomFromDate, CustomToDate)
        };
    }

    private static decimal? ParseNullableDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        const NumberStyles style = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        if (decimal.TryParse(value.Trim(), style, CultureInfo.InvariantCulture, out var invariant))
        {
            return invariant;
        }

        if (decimal.TryParse(value.Trim(), style, new CultureInfo("es-ES"), out var spanish))
        {
            return spanish;
        }

        return null;
    }

    private void UpdateSelectedTransactionDetail()
    {
        if (SelectedLedgerItem is null || !_ledgerEntriesById.TryGetValue(SelectedLedgerItem.Id, out var ledgerEntry))
        {
            SelectedTransactionDetail = null;
            return;
        }

        SelectedTransactionDetail = new TransactionDetailViewModel(
            ledgerEntry.Id,
            ledgerEntry.BookingDate.ToString("yyyy-MM-dd"),
            ledgerEntry.ValueDate?.ToString("yyyy-MM-dd"),
            FormatDescriptionForDisplay(ledgerEntry.Description),
            ledgerEntry.NormalizedDescription,
            ledgerEntry.CategoryName ?? "(Uncategorized)",
            ledgerEntry.SourceAccount ?? string.Empty,
            ledgerEntry.ExternalReference ?? string.Empty,
            ledgerEntry.Amount.ToString("0.00"));
    }

    private string FormatDescriptionForDisplay(string description)
    {
        var value = description;
        var orderedPrefixes = PrefixFilters
            .Select(static item => item.Value)
            .Where(static prefix => !string.IsNullOrWhiteSpace(prefix))
            .OrderByDescending(static prefix => prefix.Length)
            .ToArray();

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

    private static string BuildWarningBreakdown(IReadOnlyList<BankStatementParseWarning> warnings)
    {
        if (warnings.Count == 0)
        {
            return "Warnings: 0";
        }

        var grouped = warnings
            .GroupBy(static warning => warning.Code)
            .OrderBy(static warningGroup => warningGroup.Key)
            .Select(static warningGroup => $"{warningGroup.Key}={warningGroup.Count()}");

        return $"Warnings: {warnings.Count} ({string.Join(", ", grouped)})";
    }

    private CategoryOptionViewModel? GetDefaultPotentialRuleCategory()
    {
        var noneCategory = ExpenseCategories.FirstOrDefault(category =>
            string.Equals(category.Name, "None", StringComparison.OrdinalIgnoreCase));
        if (noneCategory is not null)
        {
            return noneCategory;
        }

        var preferred = ExpenseCategories.FirstOrDefault(category =>
            !string.Equals(category.Name, "Uncategorized", StringComparison.OrdinalIgnoreCase));

        return preferred ?? ExpenseCategories.FirstOrDefault() ?? Categories.FirstOrDefault();
    }

    private void RaiseCommands()
    {
        (InitializeCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (ImportCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (ApplyRulesCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (RefreshCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (BackupCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (RestoreCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (ExportCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (ImportPackageCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (ApplyFiltersCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (RefreshRulesQueueCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (NextRulesQueuePageCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (PreviousRulesQueuePageCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (ClearAllRulesCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (ClearAllTransactionsCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (MergeMatchedTransactionsCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (SavePotentialRuleCommand as DelegateCommand<EditableRulesImportedTransactionViewModel>)?.RaiseCanExecuteChanged();
        (AddPrefixFilterCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (RemovePrefixFilterCommand as DelegateCommand<PrefixFilterItemViewModel>)?.RaiseCanExecuteChanged();
        (ResetTransactionFiltersCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (NextPageCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (PreviousPageCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (ManualRecategorizeCommand as DelegateCommand)?.RaiseCanExecuteChanged();
    }

    private bool CanMoveNextPage()
    {
        if (_totalMatchedCount <= 0)
        {
            return false;
        }

        return _currentPage * PageSize < _totalMatchedCount;
    }

    private bool CanMergeMatchedTransactions()
        => MatchedImportTransactions.Count > 0
           && !string.IsNullOrWhiteSpace(_stagedMatchedFileContent)
           && !string.IsNullOrWhiteSpace(_stagedMatchedSourceName);

    private bool CanMoveNextRulesQueuePage()
    {
        if (_rulesQueueTotalMatchedCount <= 0)
        {
            return false;
        }

        return _rulesQueueCurrentPage * RulesQueuePageSize < _rulesQueueTotalMatchedCount;
    }

    private void SetSelectedView(string selectedView)
    {
        if (string.Equals(_selectedView, selectedView, StringComparison.Ordinal))
        {
            return;
        }

        _selectedView = selectedView;
        OnPropertyChanged(nameof(IsDashboardViewSelected));
        OnPropertyChanged(nameof(IsImportFiltersViewSelected));
        OnPropertyChanged(nameof(IsLedgerViewSelected));
        OnPropertyChanged(nameof(IsUtilityViewSelected));
    }
}
