using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using HomeCharts.Misc;
using HomeCharts.Models;
using HomeCharts.Models.Budget;
using HomeCharts.Repositories;
using HomeCharts.Services;
using HomeCharts.Services.Import;
using LiveCharts;
using LiveCharts.Wpf;

namespace HomeCharts.ViewModels
{
    public class ExpensesViewModel : ViewModelBase
    {
        private SeriesCollection _ytdExpenses;
        public SeriesCollection YTDExpenses
        {
            get => _ytdExpenses;
            set
            {
                _ytdExpenses = value;
                OnPropertyChanged(nameof(YTDExpenses));
            }
        }
        
        private List<string> _monthLabels;
        public List<string> MonthLabels
        {
            get => _monthLabels;
            set
            {
                _monthLabels = value;
                OnPropertyChanged(nameof(MonthLabels));
            }
        }
        
        public List<CategoryItemModel> SelectedItems
        {
            get { return _selectedItems; }
            set
            {
                _selectedItems = value;
            }
        }
        
        private CategoryItemModel _selectedItem;
        public CategoryItemModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                _categoryTransactionList.UpdateTransactionList(_selectedItem, _budget);
            }
        }
        
        private BudgetTimeFilter _timeFilter;
        public BudgetTimeFilter TimeFilter
        {
            get { return _timeFilter; }
            set
            {
                _timeFilter = value;
            }
        }
        
        
        private Dictionary<int, int> _expensesByMonth;
        private Dictionary<BudgetCategory, BudgetCategoryViewModel> _budget;
        private Dictionary<BudgetCategory, int> _medianByCategories;

        private List<CategoryItemModel> _selectedItems;
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        private CategoryExpenseListViewModel _categoryExpenseList;
        public CategoryExpenseListViewModel CategoryExpenseList => _categoryExpenseList;
        
        
        private CategoryTransactionListViewModel _categoryTransactionList;
        public CategoryTransactionListViewModel CategoryTransactionList => _categoryTransactionList;


        public ExpensesViewModel()
        {
            RemoveTransactionCommand = new RelayCommand<CategoryFilterItemModel>(OnRemoveTransactionClicked);

            _categoryExpenseList = new CategoryExpenseListViewModel();
            _categoryTransactionList = new CategoryTransactionListViewModel();
            
            var transactions = AccountService.Instance.GetAccountTransactions();

            StartDate = AccountService.Instance.GetAccountFirstTransactionDate();
            EndDate = AccountService.Instance.GetAccountLastTransactionDate();
            TimeFilter = new BudgetTimeFilter
            {
                Type = BudgetTimeFilterType.All,
                StartDate = StartDate,
                EndDate = EndDate
            };

            InitExpensesByMonth();
            UpdateView(transactions, TimeFilter, true);
            UpdateYTDExpenses();
        }
        
        public void UpdateView(List<BudgetTransaction> transactions = null, BudgetTimeFilter filter = null,
            bool updateMonthExpenses = false)
        {
            
            if (filter == null)
                filter = TimeFilter;
            if (transactions == null)
                transactions = AccountService.Instance.GetAccountTransactions();
            
            _budget = new Dictionary<BudgetCategory, BudgetCategoryViewModel>();
            InitExpensesByMonth();
            
            UpdateBudgetWithFilter(filter, transactions, updateMonthExpenses);
            UpdateExpensesMedianValue(_budget);
            
            _categoryExpenseList.UpdateExpenses(_budget, filter.Type);
            _categoryTransactionList.UpdateTransactionList(_selectedItem, _budget);
        }

        public void UpdateYTDExpenses()
        {
            var labels = new List<string>();
            var monthValues = new ChartValues<int>();
            
            foreach (var pair in _expensesByMonth)
            {
                var month = DateTimeFormatInfo.CurrentInfo?.GetAbbreviatedMonthName(pair.Key);
                if (!labels.Contains(month))
                {
                    labels.Add(month);
                }
                monthValues.Add(Math.Abs(pair.Value));
            }
            MonthLabels = labels;
            YTDExpenses = new SeriesCollection
            {
                new ColumnSeries
                {
                    Values = monthValues
                }
            };
        }
        
        private void UpdateBudgetWithFilter(BudgetTimeFilter filter, List<BudgetTransaction> transactions, bool updateMonthExpenses = false)
        {
            for (var i = transactions.Count - 1; i >= 0; i--)
            {
                var transaction = transactions[i];

                if (!IsValid(transaction, filter))
                {
                    continue;
                }

                AddExpenseIntoBudget(transaction, _budget);
                
                if (updateMonthExpenses)
                {
                    if (BudgetUtils.ContainsSalaryOrInvestment(transaction.Category))
                    {
                        continue;
                    }
                    
                    _expensesByMonth[transaction.Date.Month] += transaction.Value;
                }
            }
        }

        private void UpdateExpensesMedianValue(Dictionary<BudgetCategory, BudgetCategoryViewModel> budgetModel)
        {
            if (budgetModel.ContainsKey(BudgetCategory.Salary))
            {
                budgetModel.Remove(BudgetCategory.Salary);
            }

            foreach (var categoryViewModel in budgetModel)
            {
                categoryViewModel.Value.MedianExpenses = BudgetUtils.Median(categoryViewModel.Value.Transactions.Select(t => t.Value));
            }
        }

        private void AddExpenseIntoBudget(BudgetTransaction transaction, Dictionary<BudgetCategory, BudgetCategoryViewModel> budgetModel)
        {
            if (!budgetModel.ContainsKey(transaction.Category))
            {
                budgetModel.Add(transaction.Category, new BudgetCategoryViewModel
                {
                    Transactions = new List<BudgetTransaction>()
                });
            }

            budgetModel[transaction.Category].Transactions.Add(transaction);
            budgetModel[transaction.Category].TotalExpenses += transaction.Value;
        }

        private bool IsValid(BudgetTransaction transaction, BudgetTimeFilter filter)
        {
            switch (filter.Type)
            {
                case BudgetTimeFilterType.All:
                    return true;
                case BudgetTimeFilterType.Month:
                case BudgetTimeFilterType.DateRange:
                    return transaction.Date.Date >= filter.StartDate.Date && transaction.Date.Date <= filter.EndDate.Date;
            }
            
            return false;
        }
        
        public ICommand RemoveTransactionCommand { get; }
        
        private void OnRemoveTransactionClicked(CategoryFilterItemModel item)
        {   
            AccountDBRepository.Instance.RemoveTransactions(item);
            // ImportService.Instance.AddExpenseFilter(item.SelectedCategory, item.Vendor);
        }
        
        private void InitExpensesByMonth()
        {
            _expensesByMonth = new Dictionary<int, int>
            {
                { 1, 0 },
                { 2, 0 },
                { 3, 0 },
                { 4, 0 },
                { 5, 0 },
                { 6, 0 },
                { 7, 0 },
                { 8, 0 },
                { 9, 0 },
                { 10, 0 },
                { 11, 0 },
                { 12, 0 }
            };
        }
    }
}