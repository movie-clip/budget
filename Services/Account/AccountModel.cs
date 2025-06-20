using System.Collections.Generic;
using HomeCharts.Misc;
using HomeCharts.Models;
using HomeCharts.Models.Budget;
using HomeCharts.ViewModels;

namespace HomeCharts.Services
{
    public class AccountModel
    {
        public BudgetModel BudgetModel { get; set; }
        
        public void UpdateBudgetWithFilter(List<BudgetTransaction> transactions)
        {
            // for (var i = transactions.Count - 1; i >= 0; i--)
            // {
            //     var transaction = transactions[i];
            //     
            //     AddExpenseIntoBudget(transaction, _budget);
            //     
            //     if (BudgetUtils.ContainsSalaryOrInvestment(transaction.Category))
            //     {
            //         continue;
            //     }
            //     _expensesByMonth[transaction.Date.Month] += transaction.Value;
            // }
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
    }
}