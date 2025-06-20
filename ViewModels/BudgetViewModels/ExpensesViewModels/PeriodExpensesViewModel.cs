using System;
using System.Collections.Generic;
using System.Linq;
using HomeCharts.Models;

namespace HomeCharts.ViewModels
{
    public class PeriodExpensesViewModel : ViewModelBase
    {
        private string _periodChange;
        public string PeriodChange
        {
            get => _periodChange;
            set
            {
                _periodChange = value;
                OnPropertyChanged(nameof(PeriodChange));
            }
        }
        
        private int _periodExpenses;
        public int PeriodExpenses
        {
            get => _periodExpenses;
            set
            {
                _periodExpenses = value;
                OnPropertyChanged(nameof(PeriodExpenses));
            }
        }
        
        private int _periodIncome;
        public int PeriodIncome
        {
            get => _periodIncome;
            set
            {
                _periodIncome = value;
                OnPropertyChanged(nameof(PeriodIncome));
            }
        }

        public void UpdateData(Dictionary<BudgetCategory,BudgetCategoryViewModel> budget)
        {
            var totalExpenses = budget.Where(x => ExpenseUtils.IsValidExpense(x.Key)).Select(x => Math.Abs(x.Value.TotalExpenses)).Sum();
            PeriodExpenses = totalExpenses;

            var totalIncome = budget.Where(x => x.Key == BudgetCategory.Salary).Select(x => x.Value.TotalExpenses)
                .Sum();
            PeriodIncome = totalIncome;

            var change = totalIncome - totalExpenses;
            PeriodChange = PeriodChange = change.ToString("+#;-#;0");
        }
    }
}