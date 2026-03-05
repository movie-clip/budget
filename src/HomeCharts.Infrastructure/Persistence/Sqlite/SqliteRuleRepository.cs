using HomeCharts.Contracts.Persistence;
using HomeCharts.Domain.Model;

namespace HomeCharts.Infrastructure.Persistence.Sqlite;

public sealed class SqliteRuleRepository(IAppDbConnectionFactory connectionFactory) : IRuleRepository
{
    private readonly IAppDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task<IReadOnlyList<CategorizationRule>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var rules = new List<CategorizationRule>();

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, pattern, match_type, priority, category_id, is_active, created_utc, updated_utc
            FROM categorization_rules
            WHERE is_active = 1
            ORDER BY priority DESC, name ASC;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rules.Add(new CategorizationRule
            {
                Id = Guid.Parse(reader.GetString(0)),
                Name = reader.GetString(1),
                Pattern = reader.GetString(2),
                MatchType = (RuleMatchType)reader.GetInt32(3),
                Priority = reader.GetInt32(4),
                CategoryId = Guid.Parse(reader.GetString(5)),
                IsActive = SqliteMapping.FromBit(reader.GetInt64(6)),
                CreatedUtc = SqliteMapping.FromIsoDateTimeOffset(reader.GetString(7)),
                UpdatedUtc = SqliteMapping.FromIsoDateTimeOffset(reader.GetString(8))
            });
        }

        return rules;
    }

    public async Task UpsertAsync(CategorizationRule rule, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO categorization_rules(id, name, pattern, match_type, priority, category_id, is_active, created_utc, updated_utc)
            VALUES($id, $name, $pattern, $matchType, $priority, $categoryId, $isActive, $createdUtc, $updatedUtc)
            ON CONFLICT(id) DO UPDATE SET
                name = excluded.name,
                pattern = excluded.pattern,
                match_type = excluded.match_type,
                priority = excluded.priority,
                category_id = excluded.category_id,
                is_active = excluded.is_active,
                updated_utc = excluded.updated_utc;
            """;

        AddParameter(command, "$id", rule.Id.ToString("D"));
        AddParameter(command, "$name", rule.Name);
        AddParameter(command, "$pattern", rule.Pattern);
        AddParameter(command, "$matchType", (int)rule.MatchType);
        AddParameter(command, "$priority", rule.Priority);
        AddParameter(command, "$categoryId", rule.CategoryId.ToString("D"));
        AddParameter(command, "$isActive", SqliteMapping.ToBit(rule.IsActive));
        AddParameter(command, "$createdUtc", SqliteMapping.ToIso(rule.CreatedUtc));
        AddParameter(command, "$updatedUtc", SqliteMapping.ToIso(rule.UpdatedUtc));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM categorization_rules WHERE id = $id;";
        AddParameter(command, "$id", ruleId.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
