using System;
using System.Collections.Generic;
using HomeCharts.Models.Budget;

namespace HomeCharts.Models
{
    public enum BudgetCategory
    {
        None,
        Rent,
        Services,
        Transport,
        Grocery,
        Cafe,
        Family,
        Furniture,
        Travels,
        Smoke,
        Sasha,
        Shopping,
        Medicine,
        Education,
        Entertainment,
        Cosmetic,
        Donation,
        Commissions,
        Investments,
        Salary,
        Home
    }
    
    public class BudgetModel
    {
        public List<BudgetTransaction> Transactions { get; set; }
        public DateTime FirstDate { get; set; }
        public DateTime LastDate { get; set; }
    }
}