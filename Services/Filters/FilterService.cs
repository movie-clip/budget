using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using FileHelpers;
using HomeCharts.Models;
using HomeCharts.Models.Budget;
using HomeCharts.Repositories;
using HomeCharts.Services.Import;

namespace HomeCharts.Services.Filters
{
    public class FilterService
    {
        
        private static FilterService _instance;

        public static FilterService Instance
        {
            get
            {
                if (_instance == null)
                {
                    return new FilterService();
                }

                return _instance;
            }
        }
        
        public FilterService()
        {
            _instance = this;
            _parser = new SabadellTransactionParser();
        }
        
        private CategoryFiltersContainer _filters;
        public CategoryFiltersContainer Filters => _filters;
        
        private string[] vendorFilters = {"COMPRA TARJ. 5402XXXXXXXX7020", "COMPRA TARJ. F5402XXXXXXXX6021", "COMPRA TARJ. 5402XXXXXXXX6013"};
        
        private ITransactionParser _parser;

        public void ReadCategoriesFromDB()
        {
            _filters = ExpenseFilterRepository.Instance.ReadCategories();
        }
        
        public void AddExpenseFilter(string id, BudgetCategory category, string vendor)
        {
            ExpenseFilterRepository.Instance.WriteExpensesFilter(category, vendor);
            _filters.Filters.Add(vendor, new FilterViewModel
            {
                Id = id,
                Category = category,
            });
        }
        
        
        public void ApplyFiltersToTransactions(List<BudgetTransaction> transactions)
        {
            foreach (var transaction in transactions)
            {
                transaction.Category = GetCategory(transaction.Vendor);
            }
        }
        
        public List<BudgetTransaction> ImportFromFile(string filePath)
        {
            var fileHelperEngine = new FileHelperEngine<Record>();
            var records = fileHelperEngine.ReadFile(filePath);
            var transactions = _parser.Parse(records);
            
            return transactions;
        }
        
        private BudgetCategory GetCategory(string vendor)
        {
            foreach (var filter in _filters.Filters)
            {
                if (vendor.ToLower().Contains(filter.Key.ToLower()))
                {
                    return filter.Value.Category;
                }
            }

            return BudgetCategory.None;
        }
        
        public void RemoveTransactions(string id)
        {
            ExpenseFilterRepository.Instance.RemoveFilter(id);
        }
    }
}