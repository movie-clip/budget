using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HomeCharts.Models.Budget;
using HomeCharts.ViewModels;
using HomeCharts.ViewModels.Dashboard;
using LiveCharts;
using LiveCharts.Wpf;

namespace HomeCharts.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void OnFilterClick(object sender, RoutedEventArgs e)
        {
            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please pick both start and end dates.");
                return;
            }

            var start = StartDatePicker.SelectedDate.Value;
            var end = EndDatePicker.SelectedDate.Value;
            if (end < start)
            {
                MessageBox.Show("End must be on or after start.");
                return;
            }

            var dataContext = DataContext as DashboardViewModel;
            dataContext.TimeFilter.StartDate = start;
            dataContext.TimeFilter.EndDate = end;
            dataContext.TimeFilter.Type = BudgetTimeFilterType.DateRange;
            dataContext.UpdateView();
        }
        
        private void OnAllClick(object sender, RoutedEventArgs e)
        {
            var dataContext = DataContext as DashboardViewModel;
            dataContext.TimeFilter.Type = BudgetTimeFilterType.All;
            dataContext.UpdateView();
        }

        private void Chart_OnDataClick(object sender, ChartPoint chartpoint)
        {
            var start = StartDatePicker.SelectedDate.Value;
            var monthIndex = (int)chartpoint.X + 1;
            
            var dataContext = DataContext as DashboardViewModel;
            dataContext.TimeFilter.StartDate = new DateTime(start.Year, monthIndex, 1);
            dataContext.TimeFilter.EndDate = new DateTime(start.Year, monthIndex, DateTime.DaysInMonth(start.Year, monthIndex));
            dataContext.TimeFilter.Type = BudgetTimeFilterType.Month;
            dataContext.UpdateView();
        }
        
        private void SomeListViewList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mySelectedItems = new List<CategoryItemModel>();
            
            foreach (CategoryItemModel item in SomeListViewList.SelectedItems)
            {
                mySelectedItems.Add(item);
            }

            // (DataContext as DashboardViewModel).SelectedItems = mySelectedItems;
            // (DataContext as DashboardViewModel).SelectedItem = SomeListViewList.SelectedItem as CategoryItemModel;
        }

        private void LegendItem_Click(object sender, MouseButtonEventArgs e)
        {
            if(sender is FrameworkElement fe && fe.DataContext is Series series)
            {
                series.Visibility = series.Visibility == Visibility.Visible
                    ? Visibility.Hidden
                    : Visibility.Visible;
            }
        }
    }
}
