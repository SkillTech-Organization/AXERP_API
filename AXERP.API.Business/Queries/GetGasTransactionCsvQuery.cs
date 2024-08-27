using AXERP.API.Domain;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.ServiceContracts.Requests.Transactions;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;
using AXERP.API.Persistence.Utils;
using Dapper;
using System.Text;

namespace AXERP.API.Business.Queries
{
    [ForSystem("Google Sheet", LogConstants.FUNCTION_GOOGLE_SYNC)]
    public class GetGasTransactionCsvQuery : BaseAuditedClass<GetGasTransactionCsvQuery>
    {
        private readonly UnitOfWorkFactory _uowFactory;

        public GetGasTransactionCsvQuery(
            AxerpLoggerFactory axerpLoggerFactory,
            UnitOfWorkFactory uowFactory) : base(axerpLoggerFactory)
        {
            _uowFactory = uowFactory;
        }

        public byte[] Execute(GasTransactionCsvRequest request)
        {
            using (var uow = _uowFactory.Create())
            {
                var builder = new SqlBuilder();

                var orderByMode = request.OrderDesc ? "desc" : "";
                builder
                    .OrderBy($"{request.Order} {orderByMode}");

                if (request.FromDate.HasValue)
                {
                    builder.Where($"{nameof(Delivery.DateDelivered)} >= @dateFrom", new { dateFrom = request.FromDate.Value });
                }

                if (request.ToDate.HasValue)
                {
                    builder.Where($"{nameof(Delivery.DateDelivered)} <= @dateTo", new { dateTo = request.ToDate.Value });
                }

                var tmp = builder.AddTemplate(
                    @$"SELECT * FROM {typeof(Delivery).GetTableName()} /**where**/ /**orderby**/"
                );

                var deliveries = uow.Connection.Query<Delivery>(tmp.RawSql, tmp.Parameters);

                var allowedColumns = request.Columns.Select(x => x.ToLower());

                var cols = typeof(Delivery).GetColumnDatas();
                cols = cols.Where(x => allowedColumns.Contains(x.Name.ToLower())).ToList();

                var modelProps = typeof(Delivery).GetProperties();
                var filteredProps = modelProps.Where(x => allowedColumns.Contains(x.Name.ToLower())).ToList();

                var headerNames = cols.Select(x => x.Title);
                var cssHeader = string.Join(";", headerNames);

                var strBuilder = new System.Text.StringBuilder();
                strBuilder.
                     Append(cssHeader)
                     .Append("\r\n");

                foreach (var item in deliveries)
                {
                    List<string> csvRow = new List<string>();

                    foreach (var pi in filteredProps)
                    {
                        var sField = "";
                        var val = pi.GetValue(item, null);
                        if (val != null)
                        {
                            if (val.GetType() == typeof(DateTime))
                            {
                                var dt = (DateTime)val;
                                sField = dt.ToString("G");
                            }
                            else
                            {
                                sField = val.ToString();
                            }
                        }
                        csvRow.Add(formatCSVField(sField));

                    }
                    strBuilder.Append(string.Join(";", csvRow.ToArray())).Append("\r\n");
                }

                return Encoding.Default.GetBytes(strBuilder.ToString());

                //byte[] byteArray = Encoding.Default.GetBytes(strBuilder.ToString());
                //var stream = new MemoryStream(byteArray);

                //return stream;
            }
        }

        private static string formatCSVField(string data)
        {
            return string.Format("\"{0}\"",
                data.Replace("\"", "\"\"\"")
                .Replace("\n", "")
                .Replace(";", ",")
                .Replace("\r", "")
                );
        }
    }
}
