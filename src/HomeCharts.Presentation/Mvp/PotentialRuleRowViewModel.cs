using System.Windows.Input;
using HomeCharts.Application.UseCases;

namespace HomeCharts.Presentation.Mvp;

public sealed class PotentialRuleRowViewModel : ObservableObject
{
    private readonly string _suggestedPattern;
    private readonly Guid? _suggestedCategoryId;
    private readonly DelegateCommand _saveCommand;
    private string _description;
    private CategoryOptionViewModel? _selectedCategory;

    public PotentialRuleRowViewModel(
        PotentialRuleRow row,
        IReadOnlyList<CategoryOptionViewModel> categories,
        Func<PotentialRuleRowViewModel, Task> saveAsync,
        Func<bool> isListBusy)
    {
        RowKey = row.RowKey;
        BookingDateText = row.BookingDate.ToString("yyyy-MM-dd");
        AmountText = row.Amount.ToString("0.00");
        Categories = categories;
        _suggestedPattern = row.SuggestedPattern;
        _suggestedCategoryId = row.SuggestedCategoryId;
        _description = row.SuggestedPattern;
        _selectedCategory = row.SuggestedCategoryId is { } id
            ? categories.FirstOrDefault(category => category.Id == id)
            : null;
        _saveCommand = new DelegateCommand(
            () => _ = saveAsync(this),
            () => !isListBusy() && SelectedCategory is not null);
    }

    public string RowKey { get; }
    public string BookingDateText { get; }
    public string AmountText { get; }
    public IReadOnlyList<CategoryOptionViewModel> Categories { get; }
    public ICommand SaveCommand => _saveCommand;

    public string Description
    {
        get => _description;
        set
        {
            if (SetProperty(ref _description, value))
            {
                OnPropertyChanged(nameof(IsModified));
            }
        }
    }

    public CategoryOptionViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                OnPropertyChanged(nameof(IsModified));
                _saveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public PotentialRuleDraft Draft =>
        new(_suggestedPattern, _suggestedCategoryId, Description, SelectedCategory?.Id);

    public bool IsModified => Draft.IsModified;

    internal void RaiseCanExecuteChanged() => _saveCommand.RaiseCanExecuteChanged();
}
