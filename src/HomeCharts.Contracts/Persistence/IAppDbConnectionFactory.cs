using System.Data.Common;

namespace HomeCharts.Contracts.Persistence;

public interface IAppDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
