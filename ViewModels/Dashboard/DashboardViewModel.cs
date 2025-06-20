using System;
using System.Collections.Generic;
using System.Globalization;
using HomeCharts.Misc;
using HomeCharts.Models;
using HomeCharts.Models.Budget;
using HomeCharts.Repositories;
using HomeCharts.Services;
using LiveCharts;
using LiveCharts.Wpf;

namespace HomeCharts.ViewModels.Dashboard
{
    public class DashboardViewModel : ViewModelBase
    {
        private BudgetTimeFilter _timeFilter;
        public BudgetTimeFilter TimeFilter
        {
            get { return _timeFilter; }
            set
            {
                _timeFilter = value;
            }
        }
        
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

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        
        public CategoryExpenseListViewModel CategoryExpenseList { get; private set; }
        public PeriodExpensesViewModel PeriodExpenses { get; private set; }
        
        private Dictionary<BudgetCategory, BudgetCategoryViewModel> _budget;
        private Dictionary<int, int> _expensesByMonth;
        private Dictionary<int, int> _incomesByMonth;
        
        public DashboardViewModel()
        {
            InitCommands();
            InitViewModels();
            ResetExpensesByMonth();
            
            InitData();

            UpdateView(TimeFilter, true);
            UpdateYTDExpenses();
        }
        
        public void UpdateView(BudgetTimeFilter filter = null, bool updateMonthExpenses = false)
        {
            filter ??= TimeFilter;
            
            ResetExpensesByMonth();

            _budget = new Dictionary<BudgetCategory, BudgetCategoryViewModel>();
            var transactions = AccountService.Instance.GetAccountTransactions();
            UpdateBudgetWithFilter(filter, transactions, updateMonthExpenses);
            
            CategoryExpenseList.UpdateExpenses(_budget, filter.Type);
            PeriodExpenses.UpdateData(_budget);
        }
        
        public void UpdateYTDExpenses()
        {
            var labels = new List<string>();
            var expenses = new ChartValues<int>();
            
            foreach (var pair in _expensesByMonth)
            {
                expenses.Add(Math.Abs(pair.Value));

                var month = DateTimeFormatInfo.CurrentInfo?.GetAbbreviatedMonthName(pair.Key);
                if (!labels.Contains(month))
                {
                    labels.Add(month);
                }
            }
            MonthLabels = labels;

            var incomes = new ChartValues<int>();
            foreach (var pair in _incomesByMonth)
            {
                incomes.Add(pair.Value);
            }
            
            YTDExpenses = new SeriesCollection
            {
                new StackedColumnSeries { Title = "Expenses", Values = expenses },
                new StackedColumnSeries { Title = "Income",   Values = incomes }
            };
        }
        
        private void UpdateBudgetWithFilter(BudgetTimeFilter filter, List<BudgetTransaction> transactions, bool updateMonthExpenses = false)
        {
            ResetExpensesByMonth();
            for (var i = transactions.Count - 1; i >= 0; i--)
            {
                var transaction = transactions[i];

                if (!IsValid(transaction, filter))
                {
                    continue;
                }

                AddExpenseIntoBudget(transaction, _budget);
                
                if (transaction.Category == BudgetCategory.Salary)
                {
                    if (!_incomesByMonth.ContainsKey(transaction.Date.Month))
                    {
                        _incomesByMonth.Add(transaction.Date.Month, 0);
                    }
                    _incomesByMonth[transaction.Date.Month] += transaction.Value;
                    continue;
                }
                
                if (updateMonthExpenses)
                {
                    if (BudgetUtils.ContainsSalaryOrInvestment(transaction.Category))
                    {
                        continue;
                    }

                    if (!_expensesByMonth.ContainsKey(transaction.Date.Month))
                    {
                        _expensesByMonth.Add(transaction.Date.Month, 0);
                    }
                    _expensesByMonth[transaction.Date.Month] += transaction.Value;
                }
            }
        }

        private bool IsValid(BudgetTransaction transaction, BudgetTimeFilter filter)
        {
            switch (filter.Type)
            {
                case BudgetTimeFilterType.All:
                    return true;
                case BudgetTimeFilterType.Month:
                    return true;
                case BudgetTimeFilterType.DateRange:
                    return transaction.Date.Date >= filter.StartDate.Date && transaction.Date.Date <= filter.EndDate.Date;
            }
            
            return false;
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
        
        private void InitData()
        {
            StartDate = AccountService.Instance.GetAccountFirstTransactionDate();
            EndDate = AccountService.Instance.GetAccountLastTransactionDate();

            TimeFilter = new BudgetTimeFilter
            {
                Type = BudgetTimeFilterType.All,
                StartDate = StartDate,
                EndDate = EndDate
            };
            
            _budget = new Dictionary<BudgetCategory, BudgetCategoryViewModel>();
        }

        private void InitViewModels()
        {
            CategoryExpenseList = new CategoryExpenseListViewModel();
            PeriodExpenses = new PeriodExpensesViewModel();
            _budget = new Dictionary<BudgetCategory, BudgetCategoryViewModel>();
        }

        private void InitCommands()
        {
            // RemoveTransactionCommand = new RelayCommand<CategoryFilterItemModel>(OnRemoveTransactionClicked);
        }
        
        private void ResetExpensesByMonth()
        {
            _expensesByMonth = new Dictionary<int, int>();
            _incomesByMonth = new Dictionary<int, int>();
        }
    }
}