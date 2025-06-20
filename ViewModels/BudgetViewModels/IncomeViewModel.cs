using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HomeCharts.Models;
using HomeCharts.Repositories;
using LiveCharts;
using LiveCharts.Wpf;

namespace HomeCharts.ViewModels
{
    public class IncomeViewModel : ViewModelBase
    {
        private SeriesCollection _expenses;
        private List<string> _monthLabels;
        
        public SeriesCollection Incomes
        {
            get => _expenses;
            set
            {
                _expenses = value;
                OnPropertyChanged(nameof(Incomes));
            }
        }

        public List<string> MonthLabels
        {
            get => _monthLabels;
            set
            {
                _monthLabels = value;
                OnPropertyChanged(nameof(MonthLabels));
            }
        }
        
        public IncomeViewModel()
        {
            UpdateExpensesCartesianChart();
        }
        
        private void UpdateExpensesCartesianChart()
        {
            var budget = AccountDBRepository.Instance.GetIncomes();
            if (budget == null)
            {
                return;
            }
            
            var incomes = new Dictionary<int, int>();
            for (var i = budget.Transactions.Count - 1; i >= 0; i--)
            {
                var transaction = budget.Transactions[i];
                
                if (!incomes.ContainsKey(transaction.Date.Month))
                {
                    incomes.Add(transaction.Date.Month, 0);
                }
                
                incomes[transaction.Date.Month] += transaction.Value;
            }
            
            incomes = incomes.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            
            var labels = new List<string>();
            var monthValues = new ChartValues<int>();
            foreach (var pair in incomes)
            {
                var month = DateTimeFormatInfo.CurrentInfo?.GetAbbreviatedMonthName(pair.Key);
                if (!labels.Contains(month))
                {
                    labels.Add(month);
                }
                monthValues.Add(pair.Value);
            }

            MonthLabels = labels;
            Incomes = new SeriesCollection
            {
                new ColumnSeries
                {
                    Values = monthValues
                }
            };
        }
    }
}