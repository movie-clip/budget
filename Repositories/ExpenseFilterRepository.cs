using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using HomeCharts.Models;
using HomeCharts.Models.Budget;

namespace HomeCharts.Repositories
{
    public class ExpenseFilterRepository : RepositoryBase
    {
        private static ExpenseFilterRepository _instance;

        public static ExpenseFilterRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    return new ExpenseFilterRepository();
                }

                return _instance;
            }
        }

        public ExpenseFilterRepository()
        {
            _instance = this;
        }
        
        public CategoryFiltersContainer ReadCategories()
        {
            CategoryFiltersContainer container = null;
            var toRemove = new List<string>();
            
            using (var connection = GetConnection())
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = "select * from [CategoryFilters]";

                using (var reader = command.ExecuteReader())
                {
                    container = new CategoryFiltersContainer();
                    container.Filters = new Dictionary<string, FilterViewModel>();
                    
                    while (reader.Read())
                    {
                        var key = reader["Value"].ToString();
                        container.Filters.Add(key, new FilterViewModel
                        {
                            Id = reader["Id"].ToString(),
                            Category = (BudgetCategory)reader["Category"],
                        });

                        // to remove duplicates
                        // if (!container.Filters.ContainsKey(key))
                        // {
                        //     container.Filters.Add(key, new FilterViewModel
                        //     {
                        //         Id = reader["Id"].ToString(),
                        //         Category = (BudgetCategory)reader["Category"],
                        //     });
                        // }
                        // else
                        // {
                        //     var id = reader["Id"].ToString();
                        //     toRemove.Add(id);
                        //     Console.WriteLine($"Remove filter: {id}, vendor:{key}");
                        // }
                    }
                }
            }

            // RemoveFilters(toRemove);
            
            return container;
        }

        public void RemoveFilters(List<string> filters)
        {
            if (filters == null || filters.Count == 0) 
                return;

            // Dedupe and build parameter names
            var ids        = filters.Distinct().ToList();
            var paramNames = ids.Select((_, i) => $"@vendor{i}").ToArray();

            // DELETE ... WHERE Id IN (@id0,@id1,...)
            var sql = $@"DELETE FROM CategoryFilters WHERE Id IN ({string.Join(", ", paramNames)});";

            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);

            // Add one parameter per Id
            for (int i = 0; i < ids.Count; i++)
            {
                cmd.Parameters.Add(paramNames[i], SqlDbType.NVarChar, 36).Value = ids[i];
            }

            conn.Open();
            cmd.ExecuteNonQuery();
        }
        
        public void RemoveFilters(string filter)
        {
            // DELETE ... WHERE Id IN (@id0,@id1,...)
            const string sql = @"DELETE FROM YourTableName WHERE Id = @Id;";

            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@Id", SqlDbType.NVarChar, 36).Value = filter;

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void WriteExpensesFilter(BudgetCategory category, string vendor)
        {
            using (var connection = GetConnection())
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                
                const string refcmdText = "INSERT INTO CategoryFilters (Id, Category, Value) VALUES (@Id{0},@Category{0},@Value{0});";
                int count = 0;
                string query = string.Empty;
                
                query += string.Format(refcmdText, count);
                command.Parameters.AddWithValue(string.Format("@Id{0}", count), Guid.NewGuid().ToString("N").Substring(0, 10));
                command.Parameters.AddWithValue(string.Format("@Category{0}", count), category);
                command.Parameters.AddWithValue(string.Format("@Value{0}", count), vendor);
                
                command.CommandText = query;
                command.ExecuteNonQuery();
            }
        }

        public void WriteExpensesFilter(List<CategoryFilterItemModel> list)
        {
            using (var connection = GetConnection())
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                
                const string refcmdText = "INSERT INTO CategoryFilters (Id, Category, Value) VALUES (@Id{0},@Category{0},@Value{0});";
                int count = 0;
                string query = string.Empty;

                foreach (var item in list)
                {
                    query += string.Format(refcmdText, count);
                    command.Parameters.AddWithValue(string.Format("@Id{0}", count), Guid.NewGuid().ToString());
                    command.Parameters.AddWithValue(string.Format("@Category{0}", count), item.SelectedCategory);
                    command.Parameters.AddWithValue(string.Format("@Value{0}", count), item.Vendor);
                    count++;
                }
                
                command.CommandText = query;
                command.ExecuteNonQuery();
            }
        }
        
        public void RemoveAllExpensesFilter()
        {
            using (var connection = GetConnection())
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                
                string query = "DELETE FROM CategoryFilters;";

                command.CommandText = query;
                command.ExecuteNonQuery();
            }
        }

        public void RemoveFilter(string id)
        {
            const string sql = @"DELETE FROM CategoryFilters WHERE Id = @Id;";

            using var conn = GetConnection();
            using var cmd  = new SqlCommand(sql, conn);
    
            cmd.Parameters.Add("@Id", SqlDbType.NVarChar, 10).Value = id;
            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}