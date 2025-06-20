using System.Collections.Generic;
using HomeCharts.Models.Budget;
using HomeCharts.ViewModels;

namespace HomeCharts.Models
{
    public interface IAccountRepository
    {
        BudgetModel GetIncomes();

        BudgetModel ReadAccountDB();
        
        BudgetModel LoadAccount();
        void RemoveAllTransactions();
        void SaveAccount(List<BudgetTransaction> transactions);
    }
}