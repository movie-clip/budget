using System.Windows.Media;

namespace HomeCharts.Models.Budget
{
    public class CategoryItemModel
    {
        public BudgetCategory Category { get; set; }
        public int CategoryValue { get; set; }
        public string CategoryDif { get; set; }
        public string Brush { get; set; }
    }
}