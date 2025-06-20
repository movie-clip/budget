using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using HomeCharts.Models;
using HomeCharts.Models.Budget;
using HomeCharts.Repositories;
using HomeCharts.Services;
using HomeCharts.Services.Filters;
using HomeCharts.Services.Import;
using HomeCharts.ViewModels;
using UserControl = System.Windows.Controls.UserControl;

namespace HomeCharts.Views
{
    public partial class BudgetView : UserControl
    {
        public BudgetView()
        {
            InitializeComponent();
            FilterService.Instance.ReadCategoriesFromDB();
        }

        private void OnImportClick(object sender, RoutedEventArgs e)
        {
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
            
            var filtered = transactions.Where(x => x.Category != BudgetCategory.None).ToList();
            var budget = new BudgetModel
            {
                Transactions = filtered,
                FirstDate = DateTime.MaxValue,
                LastDate = DateTime.MinValue,
            };

            foreach (var transaction in budget.Transactions)
            {
                if(budget.FirstDate > transaction.Date)
                    budget.FirstDate = transaction.Date;
                if(budget.LastDate < transaction.Date)
                    budget.LastDate = transaction.Date;
            }
            
            var timeFilter = new BudgetTimeFilter
            {
                Type = BudgetTimeFilterType.All,
                StartDate = budget.FirstDate,
                EndDate = budget.LastDate
            };
            
            AccountService.Instance.SetAccountTransactions(budget);
            UpdateExpenseView(budget.Transactions, timeFilter);
        }
        
        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            AccountService.Instance.SaveAccountToRepository();
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            AccountService.Instance.RemoveAccountTransactions();
            UpdateExpenseView();
        }

        private void UpdateExpenseView(List<BudgetTransaction> transactions = null, BudgetTimeFilter timeFilter = null)
        {
            var budgetViewModel = budgetView.DataContext as BudgetViewModel;
            (budgetViewModel.CurrentChildView as ExpensesViewModel).UpdateView(transactions, timeFilter);
        }
    }
}