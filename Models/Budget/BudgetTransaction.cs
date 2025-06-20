using System;

namespace HomeCharts.Models.Budget
{
    public class BudgetTransaction
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public BudgetCategory Category { get; set; }
        public int Value { get; set; }
        public string Vendor { get; set; }
    }
}