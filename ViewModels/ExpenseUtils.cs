using HomeCharts.Models;

namespace HomeCharts.ViewModels
{
    public static class ExpenseUtils
    {
        
        public static bool IsValidExpense(BudgetCategory category)
        {
            if (category == BudgetCategory.Salary || category == BudgetCategory.Investments)
            {
                return false;
            }
            return true;
        }
    }
}