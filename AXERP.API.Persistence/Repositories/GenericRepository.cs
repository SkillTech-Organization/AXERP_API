using AXERP.API.Persistence.Queries;
using AXERP.API.Persistence.ServiceContracts.Requests;
using AXERP.API.Persistence.ServiceContracts.Responses;
using AXERP.API.Persistence.Utils;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AXERP.API.Persistence.Repositories
{
    public class GenericRepository
    {
        private readonly ILogger<GenericRepository> _logger;

        private readonly string _connectionStringKey;

        public GenericRepository(ILogger<GenericRepository> logger, string connectionStringKey = "SqlConnectionString")
        {
            _logger = logger;
            _connectionStringKey = connectionStringKey;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(Environment.GetEnvironmentVariable(_connectionStringKey));
        }

        private bool CheckIfTableExists(string tableName, bool onlyLetters = true)
        {
            if (onlyLetters)
            {
                string pattern = @"[^a-zA-Z0-9]";
                if (Regex.IsMatch(tableName, pattern))
                {
                    return false;
                }
            }

            using (var conn = GetConnection())
            {
                string tableNameCheckQuery = "select count(1) from information_schema.tables where table_name = @tableName";
                if (conn.QuerySingle<int>(tableNameCheckQuery, new { tableName }) == 1)
                {
                    return true;
                }
            }

            return false;
        }

        private Dictionary<string, string> GetSpecificSearchPairs<RowType>(string searchString, string searchDelimeter = "|", string columnValueDelimeter = "=")
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(searchString))
            {
                return result;
            }

            var columnSearches = searchString.Split(searchDelimeter);
            foreach (var columnSearch in columnSearches)
            {
                var searchColumn =
                    !string.IsNullOrWhiteSpace(columnSearch) &&
                    columnSearch.Split(columnValueDelimeter).Length > 1 ?
                        typeof(RowType).FilterValidColumn(columnSearch.Split(columnValueDelimeter)[0]?.Trim(), true) :
                        null;
                if (!string.IsNullOrWhiteSpace(searchColumn) && !result.ContainsKey(searchColumn))
                {
                    var searchValue = columnSearch.Split(columnValueDelimeter)[1].TrimStart();
                    result[searchColumn] = searchValue;
                }
            }

            return result;
        }

        public GenericPagedQueryResponse<dynamic> PagedQuery<RowType>(PagedQueryRequest request)
        {
            var result = new List<dynamic>();
            var totalCount = 0;

            try
            {
                totalCount = CountAll<RowType>();

                var _columnsFromRequest = request.Columns?.Any() ?? false ? typeof(RowType).FilterValidColumns(request.Columns) : typeof(RowType).GetColumnNames(null);

                // Select column list
                // Make sure to "copy" with ToList so setting searchColumns won't modify by reference
                var columnsForSelect = _columnsFromRequest.Select(x => "X." + x).ToList() ?? new List<string>();
                var searchColumns = columnsForSelect.ToList();

                // Specific columns for search string
                var searchPairs = GetSpecificSearchPairs<RowType>(request.Search);
                //if (searchPairs.Keys.Count > 0)
                //{
                //    searchColumns.Clear();
                //    searchColumns.AddRange(searchPairs.Keys);
                //    request.SearchOnlyInSelectedColumns = true;
                //}

                using (var conn = GetConnection())
                {
                    var builder = new SqlBuilder();

                    // Building template
                    var selectTemplate = builder.AddTemplate(
                        // Query
                        request.QueryTemplate,

                        // Parameters
                        new DynamicParameters(new Dictionary<string, object>
                        {
                            { "@start", request.RowNumberStart },
                            { "@finish", request.RowNumberFinish },
                        })
                    );

                    foreach (string c in columnsForSelect)
                    {
                        builder.Select(c);
                    }

                    // Optional search
                    if (!string.IsNullOrWhiteSpace(request.Search))
                    {
                        if (searchPairs.Keys.Count > 0)
                        {
                            var param_idx = 0;
                            var dynamicParams = new Dictionary<string, object>();
                            foreach (var searchPair in searchPairs)
                            {
                                dynamicParams.Add($"@search{param_idx++}", searchPair.Value);
                            }

                            var valueTypes = searchPairs.Values.Select(x => x.GetValueType()).ToList();

                            builder.Where(
                                    typeof(RowType).GetSqlMultiSearchExpressionForSpecificColumns(
                                        request.SearchOnlyInSelectedColumns ? searchPairs.Keys.ToList() : null, $"@search{param_idx}", valueTypes, "_table"),
                                    new DynamicParameters(dynamicParams));
                        }
                        else
                        {
                            builder.Where(
                                typeof(RowType).GetSqlSearchExpressionForColumns(
                                    request.SearchOnlyInSelectedColumns ? searchColumns : null, "@search", request.Search.GetValueType(), "_table"),
                                new DynamicParameters(new Dictionary<string, object>
                                {
                                { "@search", request.Search }
                                }));
                        }
                    }

                    if (typeof(RowType).CheckSqlModifier(request.OrderBy, Domain.Attributes.SqlModifiers.StringNumeral))
                    {
                        builder.OrderBy(string.Format("len(_table.{0}) {1}, _table.{0} {1}", request.OrderBy, request.OrderDesc ? "desc" : "asc"));
                    }
                    else
                    {
                        builder.OrderBy(string.Format("_table.{0} {1}", request.OrderBy, request.OrderDesc ? "desc" : "asc"));
                    }


                    result = conn.Query(selectTemplate.RawSql, selectTemplate.Parameters).ToList(); // <RowType>
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error: ");
                throw;
            }

            return new GenericPagedQueryResponse<dynamic>
            {
                Data = result,
                PageIndex = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                Columns = typeof(RowType).GetColumnDatas(request.Columns)
            };
        }

        public IEnumerable<RowType> GetAll<RowType>(string tableName, SqlConnection? conn = null, SqlTransaction? transaction = null)
        {
            if (CheckIfTableExists(tableName))
            {
                if (conn != null)
                {
                    var rows = conn.Query<RowType>(string.Format(ParameterizedQueries.GetAll, tableName), transaction: transaction);
                    return rows;
                }
                using (conn = GetConnection())
                {
                    var rows = conn.Query<RowType>(string.Format(ParameterizedQueries.GetAll, tableName));
                    return rows;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public int DeleteAll(string tableName, SqlConnection? conn = null, SqlTransaction? transaction = null)
        {
            if (CheckIfTableExists(tableName))
            {
                if (conn != null)
                {
                    var affectedRows = conn.Execute(string.Format(ParameterizedQueries.DeleteAll, tableName), transaction: transaction);
                    return affectedRows;
                }
                using (conn = GetConnection())
                {
                    var affectedRows = conn.Execute(string.Format(ParameterizedQueries.DeleteAll, tableName), transaction: transaction);
                    return affectedRows;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public int CountAll(string tableName, SqlConnection? conn = null, SqlTransaction? transaction = null)
        {
            if (CheckIfTableExists(tableName))
            {
                if (conn != null)
                {
                    var rowCount = conn.ExecuteScalar<int>(string.Format(ParameterizedQueries.Count, tableName), transaction: transaction);
                    return rowCount;
                }
                using (conn = GetConnection())
                {
                    var rowCount = conn.ExecuteScalar<int>(string.Format(ParameterizedQueries.Count, tableName), transaction: transaction);
                    return rowCount;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public IEnumerable<int> GetAllIDs(string tableName, SqlConnection? conn = null, SqlTransaction? transaction = null)
        {
            if (CheckIfTableExists(tableName))
            {
                if (conn != null)
                {
                    var rowCount = conn.Query<int>(string.Format(ParameterizedQueries.GetALLIDs, tableName), transaction: transaction);
                    return rowCount;
                }
                using (conn = GetConnection())
                {
                    var rowCount = conn.Query<int>(string.Format(ParameterizedQueries.GetALLIDs, tableName), transaction: transaction);
                    return rowCount;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public IEnumerable<RowType> GetAll<RowType>(SqlConnection? conn = null, SqlTransaction? transaction = null)
        {
            var tableName = typeof(RowType).GetTableName();
            if (CheckIfTableExists(tableName))
            {
                if (conn != null)
                {
                    var rows = conn.Query<RowType>(string.Format(ParameterizedQueries.GetAll, tableName), transaction: transaction);
                    return rows;
                }
                using (conn = GetConnection())
                {
                    var rows = conn.Query<RowType>(string.Format(ParameterizedQueries.GetAll, tableName));
                    return rows;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public int DeleteAll<RowType>(SqlConnection? conn = null, SqlTransaction? transaction = null)
        {
            var tableName = typeof(RowType).GetTableName();
            if (CheckIfTableExists(tableName))
            {
                if (conn != null)
                {
                    var affectedRows = conn.Execute(string.Format(ParameterizedQueries.DeleteAll, tableName), transaction: transaction);
                    return affectedRows;
                }
                using (conn = GetConnection())
                {
                    var affectedRows = conn.Execute(string.Format(ParameterizedQueries.DeleteAll, tableName), transaction: transaction);
                    return affectedRows;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public int CountAll<RowType>(SqlConnection? conn = null, SqlTransaction? transaction = null)
        {
            var tableName = typeof(RowType).GetTableName();
            if (CheckIfTableExists(tableName))
            {
                if (conn != null)
                {
                    var rowCount = conn.ExecuteScalar<int>(string.Format(ParameterizedQueries.Count, tableName), transaction: transaction);
                    return rowCount;
                }
                using (conn = GetConnection())
                {
                    var rowCount = conn.ExecuteScalar<int>(string.Format(ParameterizedQueries.Count, tableName), transaction: transaction);
                    return rowCount;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public IEnumerable<int> GetAllIDs<RowType>(SqlConnection? conn = null, SqlTransaction? transaction = null)
        {
            var tableName = typeof(RowType).GetTableName();
            if (CheckIfTableExists(tableName))
            {
                if (conn != null)
                {
                    var rowCount = conn.Query<int>(string.Format(ParameterizedQueries.GetALLIDs, tableName), transaction: transaction);
                    return rowCount;
                }
                using (conn = GetConnection())
                {
                    var rowCount = conn.Query<int>(string.Format(ParameterizedQueries.GetALLIDs, tableName), transaction: transaction);
                    return rowCount;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }
    }
}
