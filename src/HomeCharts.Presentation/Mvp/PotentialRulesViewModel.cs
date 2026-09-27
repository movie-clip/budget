using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using HomeCharts.Application.UseCases;

namespace HomeCharts.Presentation.Mvp;

/// <summary>
/// What the list needs from the shell that owns it: the current file, the busy gate, the status line
/// and a hook to refresh views that depend on the rule set. Plumbing only, no decisions.
/// </summary>
public sealed record PotentialRulesHost(
    Func<string> GetImportFilePath,
    Func<bool> GetIsBusy,
    Func<Func<Task>, Task> RunBusyAsync,
    Action<string> SetStatus,
    Func<Task> RefreshDependentsAsync);

public sealed class PotentialRulesViewModel : ObservableObject
{
    public const int PageSize = 120;

    private readonly PreviewPotentialRulesUseCase _previewUseCase;
    private readonly ApplyPotentialRulesUseCase _applyUseCase;
    private readonly IReadOnlyList<CategoryOptionViewModel> _categories;
    private readonly PotentialRulesHost _host;
    private readonly List<PotentialRuleRowViewModel> _allRows = [];
    private readonly DelegateCommand _refreshCommand;
    private readonly DelegateCommand _nextPageCommand;
    private readonly DelegateCommand _previousPageCommand;
    private readonly DelegateCommand _applyAllCommand;
    private string _summary = "Potential new rules: 0";
    private string _pageInfo = "Page 1/1";
    private int _currentPage = 1;
    private string? _loadedFilePath;

    public PotentialRulesViewModel(
        PreviewPotentialRulesUseCase previewUseCase,
        ApplyPotentialRulesUseCase applyUseCase,
        IReadOnlyList<CategoryOptionViewModel> categories,
        PotentialRulesHost host)
    {
        _previewUseCase = previewUseCase;
        _applyUseCase = applyUseCase;
        _categories = categories;
        _host = host;

        _refreshCommand = new DelegateCommand(() => _ = LoadAsync(null, keepEdits: true), () => !_host.GetIsBusy());
        _nextPageCommand = new DelegateCommand(MoveNextPage, () => !_host.GetIsBusy() && CanMoveNextPage());
        _previousPageCommand = new DelegateCommand(MovePreviousPage, () => !_host.GetIsBusy() && _currentPage > 1);
        _applyAllCommand = new DelegateCommand(
            () => _ = ApplyAllAsync(),
            () => !_host.GetIsBusy() && _allRows.Any(static row => row.Draft.IsApplicable));
    }

    public ObservableCollection<PotentialRuleRowViewModel> Rows { get; } = [];

    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public string PageInfo
    {
        get => _pageInfo;
        private set => SetProperty(ref _pageInfo, value);
    }

    public string ApplyAllText
    {
        get
        {
            var count = _allRows.Count(static row => row.Draft.IsApplicable);
            return count > 0 ? $"Apply All ({count})" : "Apply All";
        }
    }

    public ICommand RefreshCommand => _refreshCommand;
    public ICommand NextPageCommand => _nextPageCommand;
    public ICommand PreviousPageCommand => _previousPageCommand;
    public ICommand ApplyAllCommand => _applyAllCommand;

    /// <summary>Rebuilds the list from the file; unsaved edits are dropped (import, file change, other refresh triggers).</summary>
    public Task RefreshAsync(string? fileContent = null) => LoadAsync(fileContent, keepEdits: false);

    public void RaiseCanExecuteChanged()
    {
        _refreshCommand.RaiseCanExecuteChanged();
        _nextPageCommand.RaiseCanExecuteChanged();
        _previousPageCommand.RaiseCanExecuteChanged();
        _applyAllCommand.RaiseCanExecuteChanged();
        foreach (var row in _allRows)
        {
            row.RaiseCanExecuteChanged();
        }
    }

    private async Task LoadAsync(string? fileContent, bool keepEdits)
    {
        var filePath = _host.GetImportFilePath();
        var previousRows = keepEdits && string.Equals(_loadedFilePath, filePath, StringComparison.Ordinal)
            ? _allRows.ToList()
            : [];

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                ClearRows();
                return;
            }

            fileContent = await File.ReadAllTextAsync(filePath);
        }

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            ClearRows();
            return;
        }

        var preview = await _previewUseCase.ExecuteAsync(fileContent);
        if (preview.Errors.Count > 0)
        {
            ClearRows();
            return;
        }

        var previousByKey = new Dictionary<string, PotentialRuleRowViewModel>(StringComparer.Ordinal);
        foreach (var previous in previousRows)
        {
            previousByKey.TryAdd(previous.RowKey, previous);
        }

        _allRows.Clear();
        foreach (var row in preview.PotentialRows)
        {
            var viewModel = new PotentialRuleRowViewModel(row, _categories, SaveRowAsync, _host.GetIsBusy);
            if (previousByKey.TryGetValue(row.RowKey, out var previous))
            {
                viewModel.Description = previous.Description;
                viewModel.SelectedCategory = previous.SelectedCategory;
            }

            viewModel.PropertyChanged += OnRowPropertyChanged;
            _allRows.Add(viewModel);
        }

        _loadedFilePath = filePath;
        _currentPage = 1;
        ShowPage();
        Summary = $"Potential new rules: {_allRows.Count}";
    }

    private void ClearRows()
    {
        _allRows.Clear();
        _loadedFilePath = null;
        _currentPage = 1;
        Summary = "Potential new rules: 0";
        ShowPage();
    }

    private async Task SaveRowAsync(PotentialRuleRowViewModel row)
    {
        await _host.RunBusyAsync(async () =>
        {
            var outcome = await _applyUseCase.SaveOneAsync(row.Draft);
            switch (outcome)
            {
                case PotentialRuleSaveOutcome.CategoryMissing:
                    _host.SetStatus("Select a category before saving a rule.");
                    return;
                case PotentialRuleSaveOutcome.InvalidPattern:
                    _host.SetStatus("Description is empty. Enter a matching string first.");
                    return;
                case PotentialRuleSaveOutcome.Created:
                    await LoadAsync(null, keepEdits: true);
                    await _host.RefreshDependentsAsync();
                    _host.SetStatus($"Rule saved for '{row.Description.Trim()}'.");
                    return;
                default:
                    _host.SetStatus("Matching rule already exists.");
                    return;
            }
        });
    }

    private async Task ApplyAllAsync()
    {
        await _host.RunBusyAsync(async () =>
        {
            var drafts = _allRows.Select(static row => row.Draft).ToArray();
            var result = await _applyUseCase.SaveModifiedAsync(drafts);
            if (result.ConsideredCount == 0)
            {
                _host.SetStatus("Modify at least one row before applying all rules.");
                return;
            }

            await LoadAsync(null, keepEdits: true);
            await _host.RefreshDependentsAsync();
            _host.SetStatus($"Apply all complete. Created: {result.CreatedCount}, duplicates: {result.DuplicateCount}, invalid: {result.InvalidCount}.");
        });
    }

    private void MoveNextPage()
    {
        if (!CanMoveNextPage())
        {
            return;
        }

        _currentPage++;
        ShowPage();
    }

    private void MovePreviousPage()
    {
        if (_currentPage <= 1)
        {
            return;
        }

        _currentPage--;
        ShowPage();
    }

    private bool CanMoveNextPage() => _currentPage * PageSize < _allRows.Count;

    private void ShowPage()
    {
        Rows.Clear();

        var totalPages = Math.Max(1, (int)Math.Ceiling(_allRows.Count / (double)PageSize));
        if (_currentPage > totalPages)
        {
            _currentPage = totalPages;
        }

        foreach (var row in _allRows.Skip((_currentPage - 1) * PageSize).Take(PageSize))
        {
            Rows.Add(row);
        }

        PageInfo = $"Page {_currentPage}/{totalPages}";
        OnPropertyChanged(nameof(ApplyAllText));
        RaiseCanExecuteChanged();
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PotentialRuleRowViewModel.IsModified))
        {
            OnPropertyChanged(nameof(ApplyAllText));
            _applyAllCommand.RaiseCanExecuteChanged();
        }
    }
}
