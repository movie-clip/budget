using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileHelpers;
using HomeCharts.Models;
using HomeCharts.Models.Budget;
using HomeCharts.Repositories;

namespace HomeCharts.Services.Import
{
    public class ImportTransactionsService
    {
        private static ImportTransactionsService _instance;

        public static ImportTransactionsService Instance
        {
            get
            {
                if (_instance == null)
                {
                    return new ImportTransactionsService();
                }

                return _instance;
            }
        }
        
        private string[] vendorFilters = {"COMPRA TARJ. 5402XXXXXXXX7020", "COMPRA TARJ. 5402XXXXXXXX6021", "COMPRA TARJ. 5402XXXXXXXX6013"};
        
        private ITransactionParser _parser;
        
        public ImportTransactionsService()
        {
            _instance = this;
            _parser = new SabadellTransactionParser();
        }
        
        

        
        
        
    }
}