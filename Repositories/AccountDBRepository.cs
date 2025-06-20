using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using HomeCharts.Models;
using HomeCharts.Models.Budget;
using HomeCharts.ViewModels;

namespace HomeCharts.Repositories
{
    public class AccountDBRepository : RepositoryBase, IAccountRepository
    {
        private static AccountDBRepository _instance;

        public static AccountDBRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    return new AccountDBRepository();
                }

                return _instance;
            }
        }

        public AccountDBRepository()
        {
            _instance = this;
        }
        
        public BudgetModel LoadAccount()
        {
            var budget = new BudgetModel
            {
                FirstDate = DateTime.MaxValue,
                LastDate = DateTime.MinValue
            };

            using (var connection = GetConnection())
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = "select * from [BudgetTransaction]";
                using (var reader = command.ExecuteReader())
                {
                    budget.Transactions = new List<BudgetTransaction>();
                    
                    while (reader.Read())
                    {
                        var date = (DateTime)reader["Date"];
                        budget.Transactions.Add(new BudgetTransaction
                        {
                            Id = reader["Id"].ToString(),
                            Date = date,
                            Category = (BudgetCategory) reader["Category"],
                            Value = (int) reader["Value"],
                            Vendor = reader["Vendor"].ToString(),
                        });
                        
                        if(budget.FirstDate > date)
                            budget.FirstDate = date;
                        if(budget.LastDate < date)
                            budget.LastDate = date;
                    }
                }
            }
            
            return budget;
        }
        
        public BudgetModel GetIncomes()
        {
            BudgetModel budget = null;
            using (var connection = GetConnection())
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = $"select * from [BudgetTransaction] where year(Date) = {DateTime.Now.Year}";
                
                using (var reader = command.ExecuteReader())
                {
                    budget = new BudgetModel();
                    budget.Transactions = new List<BudgetTransaction>();
                    
                    while (reader.Read())
                    {
                        var category = (BudgetCategory) reader["Category"];
                        if (category != BudgetCategory.Salary)
                        {
                            continue;
                        }
                        
                        budget.Transactions.Add(new BudgetTransaction
                        {
                            Id = reader["Id"].ToString(),
                            Date = (DateTime) reader["Date"],
                            Category = category,
                            Value = (int) reader["Value"]
                        });
                    }
                }
            }
            
            return budget;
        }

        public BudgetModel ReadAccountDB()
        {
            var budget = new BudgetModel
            {
                FirstDate = DateTime.MaxValue,
                LastDate = DateTime.MinValue
            };

            using (var connection = GetConnection())
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = "select * from [BudgetTransaction]";
                using (var reader = command.ExecuteReader())
                {
                    budget.Transactions = new List<BudgetTransaction>();
                    
                    while (reader.Read())
                    {
                        var date = (DateTime)reader["Date"];
                        budget.Transactions.Add(new BudgetTransaction
                        {
                            Id = reader["Id"].ToString(),
                            Date = date,
                            Category = (BudgetCategory) reader["Category"],
                            Value = (int) reader["Value"],
                            Vendor = reader["Vendor"].ToString(),
                        });
                        
                        if(budget.FirstDate > date)
                            budget.FirstDate = date;
                        if(budget.LastDate < date)
                            budget.LastDate = date;
                    }
                }
            }
            
            return budget;
        }

        public void RemoveTransactions(CategoryFilterItemModel item)
        {
            const string sql = @"DELETE FROM BudgetTransaction WHERE Id = @Id;";

            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
    
            cmd.Parameters.Add("@Id", SqlDbType.NVarChar, 10).Value = item.Id;
            conn.Open();
            cmd.ExecuteNonQuery();
        }
        
        public void RemoveAllTransactions()
        {
            using (var connection = GetConnection())
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = "DELETE FROM BudgetTransaction;";
                command.ExecuteNonQuery();
            }
        }

        public void SaveAccount(List<BudgetTransaction> transactions)
        {
            var table = new DataTable();
            table.Columns.Add("Id",       typeof(string));
            table.Columns.Add("Date",     typeof(DateTime));
            table.Columns.Add("Category", typeof(int));       // or typeof(string) if you store name
            table.Columns.Add("Value",    typeof(decimal));
            table.Columns.Add("Vendor",    typeof(string));

            foreach(var entry in transactions)
            {
                table.Rows.Add(
                    Guid.NewGuid().ToString("N").Substring(0, 10), // or full GUID if your column allows it
                    entry.Date,
                    (int)entry.Category,
                    entry.Value,
                    entry.Vendor
                );
            }

            // 2) Bulk‐copy into SQL Server
            using var conn = GetConnection();
            conn.Open();
            using var bulk = new SqlBulkCopy(conn)
            {
                DestinationTableName = "BudgetTransaction",
                BatchSize            = 5_000,     // tune for your workload
                BulkCopyTimeout      = 60        // seconds
            };

            // 3) Map DataTable columns → table columns
            bulk.ColumnMappings.Add("Id",       "Id");
            bulk.ColumnMappings.Add("Date",     "Date");
            bulk.ColumnMappings.Add("Category", "Category");
            bulk.ColumnMappings.Add("Value",    "Value");
            bulk.ColumnMappings.Add("Vendor",    "Vendor");

            // 4) Write!
            bulk.WriteToServer(table);
        }
        
    }
}