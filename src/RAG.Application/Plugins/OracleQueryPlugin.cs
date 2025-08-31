using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace RAG.Application.Plugins;

/// <summary>
/// Oracle SQL Plugin for Semantic Kernel - replaces empty OracleSqlPlugin
/// </summary>
public class OracleQueryPlugin
{
    private readonly IOracleService _oracleService;
    private readonly ILogger<OracleQueryPlugin> _logger;

    public OracleQueryPlugin(IOracleService oracleService, ILogger<OracleQueryPlugin> logger)
    {
        _oracleService = oracleService;
        _logger = logger;
    }

    [KernelFunction]
    [Description("Execute Oracle SQL query and return results")]
    public async Task<string> ExecuteQuery(
        [Description("SQL query to execute")] string sqlQuery,
        [Description("Maximum number of rows to return")] int maxRows = 100)
    {
        try
        {
            _logger.LogInformation("Executing Oracle query: {Query}", sqlQuery);

            // Validate query for safety
            if (!IsQuerySafe(sqlQuery))
            {
                return "Query rejected: Only SELECT statements are allowed.";
            }

            var results = await _oracleService.ExecuteQueryAsync(sqlQuery, maxRows);
            
            if (!results.Any())
            {
                return "Query executed successfully but returned no results.";
            }

            // Format results as table
            var formattedResults = FormatQueryResults(results);
            
            return $"""
                Query executed successfully. Results ({results.Count()} rows):

                {formattedResults}
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Oracle query");
            return $"Error executing query: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Get Oracle database schema information")]
    public async Task<string> GetSchemaInfo(
        [Description("Schema name to query")] string? schemaName = null)
    {
        try
        {
            var schemas = await _oracleService.GetSchemasAsync(schemaName);
            
            return $"""
                Available schemas and tables:

                {string.Join("\n", schemas.Select(s => $"**{s.SchemaName}**\n{string.Join("\n", s.Tables.Select(t => $"  • {t}"))}"))}
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schema information");
            return "Error retrieving schema information.";
        }
    }

    [KernelFunction]
    [Description("Get table structure and column information")]
    public async Task<string> GetTableStructure(
        [Description("Table name")] string tableName,
        [Description("Schema name (optional)")] string? schemaName = null)
    {
        try
        {
            var tableInfo = await _oracleService.GetTableStructureAsync(tableName, schemaName);
            
            var columnInfo = tableInfo.Columns.Select(c => 
                $"  {c.Name} ({c.DataType}) {(c.IsNullable ? "NULL" : "NOT NULL")} {(c.IsPrimaryKey ? "PK" : "")}"
            );

            return $"""
                Table: {tableInfo.FullName}
                
                Columns:
                {string.Join("\n", columnInfo)}
                
                Row Count: {tableInfo.RowCount:N0}
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting table structure for {TableName}", tableName);
            return $"Error retrieving table structure for {tableName}.";
        }
    }

    private static bool IsQuerySafe(string query)
    {
        var upperQuery = query.Trim().ToUpperInvariant();
        
        // Only allow SELECT statements
        if (!upperQuery.StartsWith("SELECT"))
            return false;

        // Block dangerous keywords
        var dangerousKeywords = new[] { "DROP", "DELETE", "UPDATE", "INSERT", "ALTER", "CREATE", "TRUNCATE", "EXEC" };
        return !dangerousKeywords.Any(keyword => upperQuery.Contains(keyword));
    }

    private static string FormatQueryResults(IEnumerable<QueryResult> results)
    {
        if (!results.Any()) return "No results.";

        var firstResult = results.First();
        var headers = string.Join(" | ", firstResult.Columns.Keys);
        var separator = string.Join("-|-", firstResult.Columns.Keys.Select(_ => "---"));
        
        var rows = results.Select(r => 
            string.Join(" | ", r.Columns.Values.Select(v => v?.ToString() ?? "NULL"))
        );

        return $"""
            {headers}
            {separator}
            {string.Join("\n", rows)}
            """;
    }
}

// Temporary interfaces - will be moved to proper layer
public interface IOracleService
{
    Task<IEnumerable<QueryResult>> ExecuteQueryAsync(string query, int maxRows);
    Task<IEnumerable<SchemaInfo>> GetSchemasAsync(string? schemaName = null);
    Task<TableInfo> GetTableStructureAsync(string tableName, string? schemaName = null);
}

public class QueryResult
{
    public Dictionary<string, object?> Columns { get; set; } = new();
}

public class SchemaInfo
{
    public string SchemaName { get; set; } = string.Empty;
    public List<string> Tables { get; set; } = new();
}

public class TableInfo
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string FullName => string.IsNullOrEmpty(Schema) ? Name : $"{Schema}.{Name}";
    public List<ColumnInfo> Columns { get; set; } = new();
    public long RowCount { get; set; }
}

public class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
}
