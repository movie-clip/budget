using System.Collections.Generic;
using HomeCharts.Models.Budget;

namespace HomeCharts.ViewModels
{
    public class BudgetCategoryViewModel
    {
        public List<BudgetTransaction> Transactions { get; set; }
        public int TotalExpenses { get; set; }
        public int MedianExpenses { get; set; }
    }
}