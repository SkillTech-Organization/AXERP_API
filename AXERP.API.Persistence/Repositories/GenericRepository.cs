using AXERP.API.Persistence.ServiceContracts.Requests;
using AXERP.API.Persistence.ServiceContracts.Responses;
using AXERP.API.Persistence.Utils;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

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

        public int Count(string queryTemplate)
        {
            int count = 0;
            using (var conn = GetConnection())
            {
                var builder = new SqlBuilder();
                var countTemplate = builder.AddTemplate(queryTemplate);
                count = conn.ExecuteScalar<int>(countTemplate.RawSql, countTemplate.Parameters);
            }
            return count;
        }

        public GenericPagedQueryResponse<dynamic> PagedQuery<RowType>(PagedQueryRequest request)
        {
            var result = new List<dynamic>();
            var totalCount = 0;

            try
            {
                totalCount = Count(request.CountTemplate);

                var _requestedColumns = request.Columns?.Any() ?? false ? typeof(RowType).FilterValidColumns(request.Columns) : typeof(RowType).GetColumnNames(null);

                // Select column list
                // Make sure to "copy" with ToList so setting _requestedColumns won't modify by reference
                var cols = _requestedColumns.Select(x => "X." + x).ToList() ?? new List<string>();

                // Specific column for search string
                var _specificSearchColumn = !string.IsNullOrWhiteSpace(request.Search) && request.Search.Split("=").Length > 0 ?
                    typeof(RowType).FilterValidColumn(request.Search.Split("=")[0]?.Trim(), true) : null;
                if (!string.IsNullOrWhiteSpace(_specificSearchColumn))
                {
                    _requestedColumns.Clear();
                    _requestedColumns.Add(_specificSearchColumn);
                    request.SearchOnlyInSelectedColumns = true;
                    request.Search = request.Search.Split("=")[1].TrimStart();
                }

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

                    foreach (string c in cols)
                    {
                        builder.Select(c);
                    }

                    // Optional search
                    if (!string.IsNullOrWhiteSpace(request.Search))
                    {
                        builder.Where(
                            typeof(RowType).GetSqlSearchExpressionForColumns(
                                request.SearchOnlyInSelectedColumns ? _requestedColumns : null, "@search", request.Search.GetValueType(), "_table"),
                            new DynamicParameters(new Dictionary<string, object>
                            {
                                { "@search", request.Search }
                            }));
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
    }
}
