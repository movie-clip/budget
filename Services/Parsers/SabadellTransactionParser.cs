using System;
using System.Collections.Generic;
using HomeCharts.Models.Budget;

namespace HomeCharts.Services.Import
{
    public class SabadellTransactionParser : ITransactionParser
    {
        private string[] vendorFilters = {"COMPRA TARJ. 5402XXXXXXXX7020", "COMPRA TARJ. 5402XXXXXXXX6021", "COMPRA TARJ. 5402XXXXXXXX6013", "DEVOLUCION TAR.5402XXXXXXXX7020"};
        
        public List<BudgetTransaction> Parse(Record[] records)
        {
            var result = new List<BudgetTransaction>();
            foreach (var record in records)
            {
                var vendor = ApplyVendorFilters(record.Vendor);
                vendor = vendor.Trim();
                result.Add(new BudgetTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = record.DateTime,
                    Vendor = vendor,
                    Value = (int) record.Value,
                });
            }

            return result;
        }
        
        private string ApplyVendorFilters(string vendor)
        {
            foreach (var filter in vendorFilters)
            {
                vendor = vendor.Replace(filter, string.Empty);
            }

            return vendor;
        }
    }
}