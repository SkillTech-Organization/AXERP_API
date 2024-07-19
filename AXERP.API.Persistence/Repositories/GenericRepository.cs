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

        public GenericPagedQueryResponse<RowType> PagedQuery<RowType>(PagedQueryRequest request)
        {
            var result = new List<RowType>();
            var totalCount = 0;

            try
            {
                totalCount = Count(request.CountTemplate);

                using (var conn = GetConnection())
                {
                    var builder = new SqlBuilder();

                    var cols = request?.Columns?.Select(x => "X." + x) ?? new List<string>();
                    var _cols = string.Join(", ", cols);

                    var selectTemplate = builder.AddTemplate(
                        // Query
                        request.QueryTemplate,
                        //@"select X.* from (
                        //    select _table.*, ROW_NUMBER() OVER (/**orderby**/) AS RowNumber 
                        //    from @table _table 
                        //    /**where**/
                        //) as X 
                        //where RowNumber between @start and @finish",

                        // Parameters
                        new DynamicParameters(new Dictionary<string, object>
                        {
                            { "@columns", _cols },
                            { "@start", request.RowNumberStart },
                            { "@finish", request.RowNumberFinish },
                        })
                    );

                    if (!string.IsNullOrWhiteSpace(request.Search))
                    {
                        builder.Where(typeof(RowType).GetSqlSearchExpressionForColumns(null, "@search", request.Search.GetValueType(), "_table"), new DynamicParameters(new Dictionary<string, object>
                        {
                            { "@search", request.Search }
                        }));
                    }

                    builder.OrderBy(string.Format("_table.{0} {1}", request.OrderBy, request.OrderDesc ? "desc" : "asc"));

                    result = conn.Query<RowType>(selectTemplate.RawSql, selectTemplate.Parameters).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error: ");
                throw;
            }

            return new GenericPagedQueryResponse<RowType>
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
