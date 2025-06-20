using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Data;
using HomeCharts.Models;
using HomeCharts.Models.Budget;

namespace HomeCharts.ViewModels
{
    public class CategoryTransactionListViewModel : ViewModelBase
    {
        private CollectionView _categoryTransactionList;
        public CollectionView CategoryTransactionList
        {
            get
            {
                return _categoryTransactionList;
            }
            set
            {
                _categoryTransactionList = value;
                OnPropertyChanged(nameof(CategoryTransactionList));
            }
        }
        
        public void UpdateTransactionList(CategoryItemModel selectedItem, Dictionary<BudgetCategory, BudgetCategoryViewModel> budgetModel)
        {
            if(selectedItem == null)
                return;
            
            var expenses = new ObservableCollection<CategoryFilterItemModel>();
            var budget = budgetModel[selectedItem.Category];
            for (var i = 0; i < budget.Transactions.Count; i++)
            {
                var transaction = budget.Transactions[i];
                if(transaction.Category != selectedItem.Category)
                    continue;
                
                expenses.Add(new CategoryFilterItemModel
                {
                    Id = transaction.Id,
                    Date = transaction.Date,
                    Vendor = transaction.Vendor,
                    Expenses = transaction.Value,
                    SelectedCategory = transaction.Category,
                });
            }

            CategoryTransactionList = new CollectionView(expenses);
        }
    }
}