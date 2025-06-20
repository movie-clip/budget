using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using HomeCharts.Models.Budget;
using HomeCharts.ViewModels;
using LiveCharts;

namespace HomeCharts.Views.BudgetViews
{
    public partial class ExpensesView : UserControl
    {        
        public ExpensesView()
        {
            InitializeComponent();
        }

        private void SomeListViewList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<CategoryItemModel> mySelectedItems = new List<CategoryItemModel>();
            
            foreach (CategoryItemModel item in SomeListViewList.SelectedItems)
            {
                mySelectedItems.Add(item);
            }

            (DataContext as ExpensesViewModel).SelectedItems = mySelectedItems;
            (DataContext as ExpensesViewModel).SelectedItem = SomeListViewList.SelectedItem as CategoryItemModel;
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

            var dataContext = DataContext as ExpensesViewModel;
            dataContext.TimeFilter.StartDate = start;
            dataContext.TimeFilter.EndDate = end;
            dataContext.TimeFilter.Type = BudgetTimeFilterType.DateRange;
            dataContext.UpdateView();
        }
        
        private void OnAllClick(object sender, RoutedEventArgs e)
        {
            var dataContext = DataContext as ExpensesViewModel;
            dataContext.TimeFilter.Type = BudgetTimeFilterType.All;
            dataContext.UpdateView();
        }

        private void Chart_OnDataClick(object sender, ChartPoint chartpoint)
        {
            var start = StartDatePicker.SelectedDate.Value;
            var monthIndex = (int)chartpoint.X + 1;
            
            var dataContext = DataContext as ExpensesViewModel;
            dataContext.TimeFilter.StartDate = new DateTime(start.Year, monthIndex, 1);
            dataContext.TimeFilter.EndDate = new DateTime(start.Year, monthIndex, DateTime.DaysInMonth(start.Year, monthIndex));
            dataContext.TimeFilter.Type = BudgetTimeFilterType.Month;
            dataContext.UpdateView();
        }
    }
}