using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using FileHelpers;
using HomeCharts.Models;
using HomeCharts.Models.Budget;
using HomeCharts.Repositories;
using HomeCharts.Services.Filters;
using HomeCharts.Services.Import;
using HomeCharts.ViewModels;
using UserControl = System.Windows.Controls.UserControl;

namespace HomeCharts.Views.Import
{
    public partial class ImportExpensesView : UserControl
    {
        public ImportExpensesView()
        {
            InitializeComponent();
            FilterService.Instance.ReadCategoriesFromDB();
        }

        private void OnImportClick(object sender, RoutedEventArgs e)
        {
            FilterService.Instance.ReadCategoriesFromDB();
            
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            fileDialog.Title = "Open text file";

            var fileName = string.Empty;
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                fileName = fileDialog.FileName;
                Console.WriteLine("Selected file: " + fileName);
            }

            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }
            
            var transactions = FilterService.Instance.ImportFromFile(fileName);
            FilterService.Instance.ApplyFiltersToTransactions(transactions);
            var unfiltered = transactions.Where(x => x.Category == BudgetCategory.None).ToList();
            
            UpdateUnfilteredExpenses(unfiltered);
        }
        
        private void UpdateUnfilteredExpenses(List<BudgetTransaction> unfiltered)
        {
            var expenses = new ObservableCollection<CategoryFilterItemModel>();
            foreach (var expense in unfiltered)
            {
                
                expenses.Add(new CategoryFilterItemModel
                {
                    Vendor = expense.Vendor,
                    Expenses = expense.Value,
                    Date = expense.Date
                });
            }
            
            (DataContext as ImportExpensesViewModel).UnfilteredExpenses = new CollectionView(expenses);
        }

        public void OnLoadFiltersFromDBClick(object sender, RoutedEventArgs e)
        {
            FilterService.Instance.ReadCategoriesFromDB();
            
            var filters = FilterService.Instance.Filters;
            var expenses = new ObservableCollection<CategoryFilterItemModel>();
            foreach (var filter in filters.Filters)
            {
                expenses.Add(new CategoryFilterItemModel
                {
                    Id = filter.Value.Id,
                    Vendor = filter.Key,
                    SelectedCategory = filter.Value.Category,
                });
            }
            (DataContext as ImportExpensesViewModel).UnfilteredExpenses = new CollectionView(expenses);
        }
        
    }
}