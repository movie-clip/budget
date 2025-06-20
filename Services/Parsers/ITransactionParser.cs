using System.Collections.Generic;
using HomeCharts.Models.Budget;

namespace HomeCharts.Services.Import
{
    public interface ITransactionParser
    {
        List<BudgetTransaction> Parse(Record[] records);
    }
}