using System;

namespace HomeCharts.ViewModels
{
    public enum BudgetTimeFilterType
    {
        All,
        Month,
        DateRange,
    }
    
    public class BudgetTimeFilter
    {
        public BudgetTimeFilterType Type { get; set; }
        public DateTime StartDate { get; set; }        
        public DateTime EndDate { get; set; }        
    }
}