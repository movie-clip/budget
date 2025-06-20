using System.Collections.Generic;

namespace HomeCharts.Models.Budget
{
    public class CategoryFiltersContainer
    {
        public Dictionary<string, FilterViewModel> Filters { get; set; }
    }

    public class FilterViewModel
    {
        public string Id { get; set; }
        public BudgetCategory Category { get; set; }
    }
}