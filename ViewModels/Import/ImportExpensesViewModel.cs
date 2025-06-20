using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Input;
using HomeCharts.Models.Budget;
using HomeCharts.Services.Filters;
using HomeCharts.Services.Import;

namespace HomeCharts.ViewModels
{
    public class ImportExpensesViewModel : ViewModelBase
    {
        public ObservableCollection<CategoryFilterItemModel> AppliedFilters { get; }
            = new ObservableCollection<CategoryFilterItemModel>();
        
        public ICommand ActionCommand { get; }
        
        public ImportExpensesViewModel()
        {
            ActionCommand = new RelayCommand<CategoryFilterItemModel>(item =>
                {
                    if (item.Expenses != 0)
                    {
                        if (!AppliedFilters.Contains(item))
                            AppliedFilters.Add(item);
                    
                        var expenses = new ObservableCollection<CategoryFilterItemModel>();
                        for (var i = 0; i < _unfilteredExpenses.Count; i++)
                        {
                            var expense = _unfilteredExpenses.GetItemAt(i) as CategoryFilterItemModel;
                            if (expense.Vendor.Contains(item.Vendor))
                            {
                                continue;
                            }
                            expenses.Add(new CategoryFilterItemModel
                            {
                                Id = expense.Id,
                                Vendor = expense.Vendor,
                                Expenses = expense.Expenses,
                                Date = expense.Date
                            });
                        }
                        
                        UnfilteredExpenses = new CollectionView(expenses);
                        FilterService.Instance.AddExpenseFilter(item.Id, item.SelectedCategory, item.Vendor);
                        return;
                    }
                    
                    FilterService.Instance.RemoveTransactions(item.Id);
                });
        }
        
        private CollectionView _unfilteredExpenses;
        public CollectionView UnfilteredExpenses
        {
            get
            {
                return _unfilteredExpenses;
            }
            set
            {
                _unfilteredExpenses = value;
                OnPropertyChanged(nameof(UnfilteredExpenses));
            }
        }
    }
}