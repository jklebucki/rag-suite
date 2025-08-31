using Oracle.ManagedDataAccess.Client;
using RAG.Application.Plugins;
using System.Data;

namespace RAG.Infrastructure.Oracle;

/// <summary>
/// Oracle service implementation with security and performance considerations
/// </summary>
public class OracleService : IOracleService
{
    private readonly string _connectionString;
    private readonly ILogger<OracleService> _logger;
    private readonly IConfiguration _configuration;

    public OracleService(IConfiguration configuration, ILogger<OracleService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("Oracle") 
            ?? throw new InvalidOperationException("Oracle connection string not found");
    }

    public async Task<IEnumerable<QueryResult>> ExecuteQueryAsync(string query, int maxRows)
    {
        _logger.LogInformation("Executing Oracle query with max rows: {MaxRows}", maxRows);
        
        try
        {
            using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new OracleCommand(query, connection);
            
            // Set timeout and security constraints
            command.CommandTimeout = _configuration.GetValue<int>("Oracle:QueryTimeoutSeconds", 30);
            
            // Add ROWNUM constraint for safety
            var constrainedQuery = $"SELECT * FROM ({query}) WHERE ROWNUM <= {maxRows}";
            command.CommandText = constrainedQuery;

            using var reader = await command.ExecuteReaderAsync();
            var results = new List<QueryResult>();

            while (await reader.ReadAsync() && results.Count < maxRows)
            {
                var row = new QueryResult();
                
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row.Columns[columnName] = value;
                }
                
                results.Add(row);
            }

            _logger.LogInformation("Query executed successfully, returned {Count} rows", results.Count);
            return results;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "Oracle error executing query: {ErrorCode}", ex.Number);
            throw new InvalidOperationException($"Database error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Oracle query");
            throw;
        }
    }

    public async Task<IEnumerable<SchemaInfo>> GetSchemasAsync(string? schemaName = null)
    {
        _logger.LogInformation("Getting schema information for: {SchemaName}", schemaName ?? "all schemas");
        
        try
        {
            using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            var query = """
                SELECT DISTINCT 
                    owner as schema_name,
                    table_name
                FROM all_tables 
                WHERE owner NOT IN ('SYS', 'SYSTEM', 'CTXSYS', 'MDSYS', 'OLAPSYS', 'ORDSYS', 'OUTLN', 'WMSYS')
                """;

            if (!string.IsNullOrEmpty(schemaName))
            {
                query += " AND UPPER(owner) = UPPER(:schemaName)";
            }

            query += " ORDER BY owner, table_name";

            using var command = new OracleCommand(query, connection);
            
            if (!string.IsNullOrEmpty(schemaName))
            {
                command.Parameters.Add(new OracleParameter("schemaName", schemaName));
            }

            using var reader = await command.ExecuteReaderAsync();
            var schemasDict = new Dictionary<string, List<string>>();

            while (await reader.ReadAsync())
            {
                var schema = reader.GetString("schema_name");
                var tableName = reader.GetString("table_name");

                if (!schemasDict.ContainsKey(schema))
                {
                    schemasDict[schema] = new List<string>();
                }
                
                schemasDict[schema].Add(tableName);
            }

            return schemasDict.Select(kvp => new SchemaInfo
            {
                SchemaName = kvp.Key,
                Tables = kvp.Value
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schema information");
            throw;
        }
    }

    public async Task<TableInfo> GetTableStructureAsync(string tableName, string? schemaName = null)
    {
        _logger.LogInformation("Getting table structure for: {SchemaName}.{TableName}", schemaName, tableName);
        
        try
        {
            using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();

            // Get column information
            var columnQuery = """
                SELECT 
                    column_name,
                    data_type,
                    nullable,
                    CASE WHEN constraint_type = 'P' THEN 'Y' ELSE 'N' END as is_primary_key
                FROM all_tab_columns atc
                LEFT JOIN (
                    SELECT acc.column_name, ac.constraint_type
                    FROM all_constraints ac
                    JOIN all_cons_columns acc ON ac.constraint_name = acc.constraint_name
                    WHERE ac.table_name = UPPER(:tableName)
                    AND (:schemaName IS NULL OR ac.owner = UPPER(:schemaName))
                    AND ac.constraint_type = 'P'
                ) pk ON atc.column_name = pk.column_name
                WHERE atc.table_name = UPPER(:tableName)
                AND (:schemaName IS NULL OR atc.owner = UPPER(:schemaName))
                ORDER BY atc.column_id
                """;

            using var columnCommand = new OracleCommand(columnQuery, connection);
            columnCommand.Parameters.Add(new OracleParameter("tableName", tableName));
            columnCommand.Parameters.Add(new OracleParameter("schemaName", schemaName ?? (object)DBNull.Value));

            var columns = new List<ColumnInfo>();
            using var reader = await columnCommand.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                columns.Add(new ColumnInfo
                {
                    Name = reader.GetString("column_name"),
                    DataType = reader.GetString("data_type"),
                    IsNullable = reader.GetString("nullable") == "Y",
                    IsPrimaryKey = reader.GetString("is_primary_key") == "Y"
                });
            }

            // Get row count
            var countQuery = schemaName != null 
                ? $"SELECT COUNT(*) FROM {schemaName}.{tableName}"
                : $"SELECT COUNT(*) FROM {tableName}";

            using var countCommand = new OracleCommand(countQuery, connection);
            var rowCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync());

            return new TableInfo
            {
                Name = tableName,
                Schema = schemaName ?? "",
                Columns = columns,
                RowCount = rowCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting table structure for {TableName}", tableName);
            throw;
        }
    }
}
