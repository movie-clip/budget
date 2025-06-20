using System;
using System.Collections.Generic;
using System.Linq;
using HomeCharts.Models;

namespace HomeCharts.Misc
{
    public static class BudgetUtils
    {
        public static int Median(this IEnumerable<int> source)
        {
            if (source == null) 
                throw new ArgumentNullException(nameof(source));

            var sorted = source.OrderBy(x => x).ToList();
            int count = sorted.Count;
            if (count == 0) 
                throw new InvalidOperationException("Cannot compute median on an empty set.");

            int mid = count / 2;
            // even: average of two middle values
            if (count % 2 == 0)
                return (sorted[mid - 1] + sorted[mid]) / 2;
            // odd: middle value
            return sorted[mid];
        }

        public static bool ContainsSalaryOrInvestment(BudgetCategory category)
        {
            return category == BudgetCategory.Salary || category == BudgetCategory.Investments;
        }
    }
}