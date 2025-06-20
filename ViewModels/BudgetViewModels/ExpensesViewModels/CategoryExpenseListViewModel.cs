using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using HomeCharts.Misc;
using HomeCharts.Models;
using HomeCharts.Models.Budget;
using LiveCharts;
using LiveCharts.Wpf;

namespace HomeCharts.ViewModels
{
    public class CategoryExpenseListViewModel : ViewModelBase
    {
        private CollectionView _categoryExpensesList;
        public CollectionView CategoryExpensesList
        {
            get
            {
                return _categoryExpensesList;
            }
            set
            {
                _categoryExpensesList = value;
                OnPropertyChanged(nameof(CategoryExpensesList));
            }
        }
        
        private SeriesCollection _pieSeries;
        public SeriesCollection PieSeries
        {
            get => _pieSeries;
            set
            {
                _pieSeries = value;
                OnPropertyChanged(nameof(PieSeries));
            }
        }
        
        public Func<ChartPoint, string> LabelPoint { get; set; }

        private SeriesCollection _selectedCategories;
        public SeriesCollection SelectedCategories
        {
            get => _selectedCategories;
            set
            {
                _selectedCategories = value;
                OnPropertyChanged(nameof(SelectedCategories));
            }
        }
        
        private List<string> _allLabels;
        public List<string> AllLabels
        {
            get => _allLabels;
            set
            {
                _allLabels = value;
                OnPropertyChanged(nameof(AllLabels));
            }
        }
        
        public CategoryExpenseListViewModel()
        {
            LabelPoint = point => $"{point.Y}({point.Participation:P})";
        }
        
        public void UpdateExpenses(Dictionary<BudgetCategory, BudgetCategoryViewModel> budget, BudgetTimeFilterType filterType)
        {
            var totalExpenses = budget.Where(x => ExpenseUtils.IsValidExpense(x.Key)).Select(x => Math.Abs(x.Value.TotalExpenses)).Sum();
            // update category list
            var expenses = new List<CategoryItemModel>();
            foreach (var pair in budget)
            {
                if (BudgetUtils.ContainsSalaryOrInvestment(pair.Key))
                {
                    continue;
                }

                var raw   = Math.Abs((float)pair.Value.TotalExpenses / totalExpenses * 100f);
                var whole = (int)Math.Round(raw);
                expenses.Add(new CategoryItemModel
                {
                    Category = pair.Key,
                    CategoryValue = pair.Value.TotalExpenses,
                    CategoryDif = $"{whole}%",
                    Brush = "#FF7986ED"
                });
            }
            
            expenses = expenses.OrderBy(x => x.CategoryValue).ToList();
            CategoryExpensesList = new CollectionView(expenses);
            
            // update pie chart
            PieSeries = new SeriesCollection();
            for (var i = 0; i < expenses.Count; i++)
            {
                var expense = expenses[i];
                PieSeries.Add(new PieSeries
                {
                    Title = expense.Category.ToString(),
                    Values = new ChartValues<int> { expense.CategoryValue },
                    DataLabels = true
                });
            }

            UpdateFilteredChart(budget, filterType);
        }
        
        private void UpdateFilteredChart(Dictionary<BudgetCategory, BudgetCategoryViewModel> budget, BudgetTimeFilterType filterType)
        {
            var expenses = new SortedDictionary<int, Dictionary<BudgetCategory, int>>();
            foreach (var pair in budget)
            {
                foreach (var transaction in pair.Value.Transactions)
                {
                    if (BudgetUtils.ContainsSalaryOrInvestment(transaction.Category))
                    {
                        continue;
                    }

                    AddExpensesByDate(expenses, transaction, filterType);
                }
            }
            
            var labels = new List<string>();
            foreach (var pair in expenses)
            {
                var index = pair.Key.ToString();
                if (filterType == BudgetTimeFilterType.All)
                {
                    index = DateTimeFormatInfo.CurrentInfo?.GetAbbreviatedMonthName(pair.Key);
                }
                
                if (!labels.Contains(index))
                {
                    labels.Add(index);
                }
            }
            
            var series = new SeriesCollection();
            foreach (var pair in budget)
            {
                var category = pair.Key;
                var item = new LineSeries
                {
                    Values = new ChartValues<int>(),
                    Title = category.ToString()
                };
                
                foreach (var expense in expenses)
                {
                    if (expense.Value.ContainsKey(category))
                    {
                        item.Values.Add(expense.Value[category]);
                    }
                }
                
                series.Add(item);
            }
            
            SelectedCategories = series;
            AllLabels = labels;
        }

        private void AddExpensesByDate(SortedDictionary<int, Dictionary<BudgetCategory,int>> expenses, BudgetTransaction transaction, BudgetTimeFilterType filterType)
        {
            var index = transaction.Date.Month;
            if (filterType == BudgetTimeFilterType.Month)
            {
                index = transaction.Date.Day;
            }
            
            if (!expenses.ContainsKey(index))
            {
                expenses.Add(index, new Dictionary<BudgetCategory, int>());
            }

            if (!expenses[index].ContainsKey(transaction.Category))
            {
                expenses[index].Add(transaction.Category, 0);
            }
            
            expenses[index][transaction.Category] += Math.Abs(transaction.Value);
        }
    }
}