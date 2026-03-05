using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Categorization;
using HomeCharts.Domain.Model;

namespace HomeCharts.Infrastructure.Persistence.Sqlite;

public sealed class SqliteTransactionRepository(IAppDbConnectionFactory connectionFactory) : ITransactionRepository
{
    private readonly IAppDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task UpsertManyAsync(IReadOnlyCollection<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        if (transactions.Count == 0)
        {
            return;
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var transaction in transactions)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = dbTransaction;
            command.CommandText = """
                INSERT INTO transactions(
                    id, booking_date, value_date, description, normalized_description, transaction_fingerprint, amount, currency, source_account,
                    counterparty, external_reference, category_id, is_deleted, created_utc, updated_utc)
                VALUES(
                    $id, $bookingDate, $valueDate, $description, $normalizedDescription, $transactionFingerprint, $amount, $currency, $sourceAccount,
                    $counterparty, $externalReference, $categoryId, $isDeleted, $createdUtc, $updatedUtc)
                ON CONFLICT(id) DO UPDATE SET
                    booking_date = excluded.booking_date,
                    value_date = excluded.value_date,
                    description = excluded.description,
                    normalized_description = excluded.normalized_description,
                    transaction_fingerprint = excluded.transaction_fingerprint,
                    amount = excluded.amount,
                    currency = excluded.currency,
                    source_account = excluded.source_account,
                    counterparty = excluded.counterparty,
                    external_reference = excluded.external_reference,
                    category_id = excluded.category_id,
                    is_deleted = excluded.is_deleted,
                    updated_utc = excluded.updated_utc;
                """;

            BindTransaction(command, transaction);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await dbTransaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(DateOnly fromInclusive, DateOnly toInclusive, CancellationToken cancellationToken = default)
    {
        var items = new List<Transaction>();

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, booking_date, value_date, description, normalized_description, transaction_fingerprint, amount, currency, source_account,
                   counterparty, external_reference, category_id, is_deleted, created_utc, updated_utc
            FROM transactions
            WHERE booking_date >= $fromDate AND booking_date <= $toDate
            ORDER BY booking_date DESC, created_utc DESC;
            """;

        AddParameter(command, "$fromDate", SqliteMapping.ToIsoDate(fromInclusive));
        AddParameter(command, "$toDate", SqliteMapping.ToIsoDate(toInclusive));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadTransaction(reader));
        }

        return items;
    }

    public async Task<Transaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, booking_date, value_date, description, normalized_description, transaction_fingerprint, amount, currency, source_account,
                   counterparty, external_reference, category_id, is_deleted, created_utc, updated_utc
            FROM transactions
            WHERE id = $id;
            """;
        AddParameter(command, "$id", transactionId.ToString("D"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadTransaction(reader);
    }

    public async Task SetCategoryAsync(Guid transactionId, Guid? categoryId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE transactions
            SET category_id = $categoryId,
                updated_utc = $updatedUtc
            WHERE id = $id;
            """;

        AddParameter(command, "$id", transactionId.ToString("D"));
        AddParameter(command, "$categoryId", categoryId?.ToString("D"));
        AddParameter(command, "$updatedUtc", SqliteMapping.ToIso(DateTimeOffset.UtcNow));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<string>> GetExistingFingerprintsAsync(IReadOnlyCollection<string> fingerprints, CancellationToken cancellationToken = default)
    {
        if (fingerprints.Count == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var distinct = fingerprints.Where(static value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).ToArray();
        if (distinct.Length == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        var parameterNames = new string[distinct.Length];
        for (var index = 0; index < distinct.Length; index++)
        {
            var parameterName = $"$fp{index}";
            parameterNames[index] = parameterName;
            AddParameter(command, parameterName, distinct[index]);
        }

        command.CommandText = $"""
            SELECT transaction_fingerprint
            FROM transactions
            WHERE is_deleted = 0
              AND transaction_fingerprint IN ({string.Join(", ", parameterNames)});
            """;

        var existing = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            existing.Add(reader.GetString(0));
        }

        return existing;
    }

    public async Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM transactions;";
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Transaction ReadTransaction(System.Data.Common.DbDataReader reader)
    {
        return new Transaction
        {
            Id = Guid.Parse(reader.GetString(0)),
            BookingDate = SqliteMapping.FromIsoDate(reader.GetString(1)),
            ValueDate = reader.IsDBNull(2) ? null : SqliteMapping.FromIsoDate(reader.GetString(2)),
            Description = reader.GetString(3),
            NormalizedDescription = reader.GetString(4),
            TransactionFingerprint = reader.GetString(5),
            Amount = reader.GetDecimal(6),
            Currency = reader.GetString(7),
            SourceAccount = reader.IsDBNull(8) ? null : reader.GetString(8),
            Counterparty = reader.IsDBNull(9) ? null : reader.GetString(9),
            ExternalReference = reader.IsDBNull(10) ? null : reader.GetString(10),
            CategoryId = reader.IsDBNull(11) ? null : Guid.Parse(reader.GetString(11)),
            IsDeleted = SqliteMapping.FromBit(reader.GetInt64(12)),
            CreatedUtc = SqliteMapping.FromIsoDateTimeOffset(reader.GetString(13)),
            UpdatedUtc = SqliteMapping.FromIsoDateTimeOffset(reader.GetString(14))
        };
    }

    private static void BindTransaction(System.Data.Common.DbCommand command, Transaction transaction)
    {
        AddParameter(command, "$id", transaction.Id.ToString("D"));
        AddParameter(command, "$bookingDate", SqliteMapping.ToIsoDate(transaction.BookingDate));
        AddParameter(command, "$valueDate", transaction.ValueDate is null ? null : SqliteMapping.ToIsoDate(transaction.ValueDate.Value));
        AddParameter(command, "$description", transaction.Description);
        var normalizedDescription = string.IsNullOrWhiteSpace(transaction.NormalizedDescription)
            ? DescriptionNormalizer.Normalize(transaction.Description)
            : transaction.NormalizedDescription;
        AddParameter(command, "$normalizedDescription", normalizedDescription);
        AddParameter(command, "$transactionFingerprint", string.IsNullOrWhiteSpace(transaction.TransactionFingerprint)
            ? TransactionFingerprintBuilder.Build(
                transaction.BookingDate,
                transaction.ValueDate,
                transaction.Description,
                transaction.Amount,
                0m,
                transaction.ExternalReference,
                transaction.SourceAccount)
            : transaction.TransactionFingerprint);
        AddParameter(command, "$amount", transaction.Amount);
        AddParameter(command, "$currency", transaction.Currency);
        AddParameter(command, "$sourceAccount", transaction.SourceAccount);
        AddParameter(command, "$counterparty", transaction.Counterparty);
        AddParameter(command, "$externalReference", transaction.ExternalReference);
        AddParameter(command, "$categoryId", transaction.CategoryId?.ToString("D"));
        AddParameter(command, "$isDeleted", SqliteMapping.ToBit(transaction.IsDeleted));
        AddParameter(command, "$createdUtc", SqliteMapping.ToIso(transaction.CreatedUtc));
        AddParameter(command, "$updatedUtc", SqliteMapping.ToIso(transaction.UpdatedUtc));
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
