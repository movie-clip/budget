using System.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace HomeCharts.Repositories
{
    public abstract class RepositoryBase
    {
        private readonly string _connectionString;

        public RepositoryBase()
        {
            _connectionString = "Server=DESKTOP-LB9AFL2\\SQLEXPRESS; Database=HomeChartDB; Integrated Security=true";
        }

        protected SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}