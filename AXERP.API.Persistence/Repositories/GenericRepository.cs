using AXERP.API.Domain.Interfaces.Repositories;
using AXERP.API.Domain.Interfaces.UnitOfWork;
using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.Persistence.Queries;
using AXERP.API.Persistence.Utils;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AXERP.API.Persistence.Repositories
{
    public class GenericRepository : IGenericRepository
    {
        private readonly IConnectionProvider _connectionProvider;
        private SqlConnection _connection => _connectionProvider.Connection;
        private SqlTransaction? _transaction => _connectionProvider.Transaction;

        public GenericRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        //TODO: static?
        public DataTable CreateDataTable<T>(IEnumerable<T> list)
        {
            Type type = typeof(T);
            var properties = type.GetProperties();

            DataTable dataTable = new DataTable();
            dataTable.TableName = typeof(T).FullName;
            foreach (PropertyInfo info in properties)
            {
                dataTable.Columns.Add(new DataColumn(info.Name, Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType));
            }

            foreach (T entity in list)
            {
                object[] values = new object[properties.Length];
                for (int i = 0; i < properties.Length; i++)
                {
                    values[i] = properties[i].GetValue(entity);
                }

                dataTable.Rows.Add(values);
            }

            return dataTable;
        }

        public void BulkCopy<T>(List<T> rows, DataRowState? state)
        {
            DataTable data = CreateDataTable<T>(rows);

            using (var bulkCopy = new SqlBulkCopy(_connection, SqlBulkCopyOptions.Default, _transaction))
            {
                bulkCopy.DestinationTableName = $"dbo.{typeof(T).GetTableName()}";
                bulkCopy.WriteToServer(data, state ?? DataRowState.Unchanged);
            }
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

            string tableNameCheckQuery = "select count(1) from information_schema.tables where table_name = @tableName";
            if (_connection.QuerySingle<int>(tableNameCheckQuery, new { tableName }) == 1)
            {
                return true;
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

            totalCount = CountAll<RowType>();

            var _columnsFromRequest = request.Columns?.Any() ?? false ? typeof(RowType).FilterValidColumns(request.Columns, true) : typeof(RowType).GetColumnNames(null);

            // Select column list
            // Make sure to "copy" with ToList so setting searchColumns won't modify by reference
            var columnsForSelect = _columnsFromRequest.Select(x => "X." + x).ToList() ?? new List<string>();
            var searchColumns = columnsForSelect.ToList();

            // Specific columns for search string
            var searchPairs = GetSpecificSearchPairs<RowType>(request.Search);
            if (searchPairs.Keys.Count > 0)
            {
                searchColumns.Clear();
                searchColumns.AddRange(searchPairs.Keys);
                request.SearchOnlyInSelectedColumns = true;
            }

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
                                request.SearchOnlyInSelectedColumns ? searchPairs.Keys.ToList() : null, $"@search", valueTypes, "_table"),
                            new DynamicParameters(dynamicParams));
                }
                else
                {
                    builder.Where(
                        typeof(RowType).GetSqlSearchExpressionForColumns(
                            request.SearchOnlyInSelectedColumns ? _columnsFromRequest : null, "@search", request.Search.GetValueType(), "_table"),
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

            result = _connection.Query(selectTemplate.RawSql, selectTemplate.Parameters).ToList(); // <RowType>

            return new GenericPagedQueryResponse<dynamic>
            {
                Data = result,
                PageIndex = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                Columns = typeof(RowType).GetColumnDatas(request.Columns)
            };
        }

        public IEnumerable<RowType> GetAll<RowType>(string tableName)
        {
            if (CheckIfTableExists(tableName))
            {
                if (_connection != null)
                {
                    var rows = _connection.Query<RowType>(string.Format(ParameterizedQueries.GetAll, tableName), transaction: _transaction);
                    return rows;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public int DeleteAll(string tableName)
        {
            if (CheckIfTableExists(tableName))
            {
                if (_connection != null)
                {
                    var affectedRows = _connection.Execute(string.Format(ParameterizedQueries.DeleteAll, tableName), transaction: _transaction);
                    return affectedRows;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public int CountAll(string tableName)
        {
            if (CheckIfTableExists(tableName))
            {
                if (_connection != null)
                {
                    var rowCount = _connection.ExecuteScalar<int>(string.Format(ParameterizedQueries.Count, tableName), transaction: _transaction);
                    return rowCount;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public IEnumerable<IdType> GetAllIDs<IdType>(string tableName)
        {
            if (CheckIfTableExists(tableName))
            {
                if (_connection != null)
                {
                    var res = _connection.Query<IdType>(string.Format(ParameterizedQueries.GetALLIDs, tableName), transaction: _transaction);
                    return res;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public IEnumerable<RowType> GetAll<RowType>()
        {
            var tableName = typeof(RowType).GetTableName();
            if (CheckIfTableExists(tableName))
            {
                if (_connection != null)
                {
                    var rows = _connection.Query<RowType>(string.Format(ParameterizedQueries.GetAll, tableName), transaction: _transaction);
                    return rows;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public int DeleteAll<RowType>()
        {
            var tableName = typeof(RowType).GetTableName();
            if (CheckIfTableExists(tableName))
            {
                if (_connection != null)
                {
                    var affectedRows = _connection.Execute(string.Format(ParameterizedQueries.DeleteAll, tableName), transaction: _transaction);
                    return affectedRows;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public int CountAll<RowType>()
        {
            var tableName = typeof(RowType).GetTableName();
            if (CheckIfTableExists(tableName))
            {
                if (_connection != null)
                {
                    var rowCount = _connection.ExecuteScalar<int>(string.Format(ParameterizedQueries.Count, tableName), transaction: _transaction);
                    return rowCount;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }

        public IEnumerable<IdType> GetAllIDs<RowType, IdType>()
        {
            var tableName = typeof(RowType).GetTableName();
            if (CheckIfTableExists(tableName))
            {
                if (_connection != null)
                {
                    var res = _connection.Query<IdType>(string.Format(ParameterizedQueries.GetALLIDs, tableName), transaction: _transaction);
                    return res;
                }
            }
            throw new Exception($"Illegal table name: {tableName}");
        }
    }
}
