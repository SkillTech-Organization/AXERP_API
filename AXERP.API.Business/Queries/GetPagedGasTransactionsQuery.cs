using AXERP.API.Domain;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.Models;
using AXERP.API.Domain.ServiceContracts.Requests.Transactions;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;
using AXERP.API.Persistence.Utils;
using Dapper;

namespace AXERP.API.Business.Queries
{
    [ForSystem("Google Sheet", LogConstants.FUNCTION_GOOGLE_SYNC)]
    public class GetPagedGasTransactionsQuery : BaseAuditedClass<GetPagedGasTransactionsQuery>
    {
        private readonly UnitOfWorkFactory _uowFactory;

        public GetPagedGasTransactionsQuery(
            AxerpLoggerFactory axerpLoggerFactory,
            UnitOfWorkFactory uowFactory) : base(axerpLoggerFactory)
        {
            _uowFactory = uowFactory;
        }

        public PagedList<Delivery> Execute(GetPagedGasTransactionsQueryRequest request)
        {
            using (var uow = _uowFactory.Create())
            {
                var builder = new SqlBuilder();

                var orderByMode = request.OrderDesc ? "desc" : "";
                builder
                    .OrderBy($"{request.OrderBy} {orderByMode}");

                if (request.FromDate.HasValue)
                {
                    builder.Where($"{nameof(Delivery.DateLoadedEnd)} >= @dateFrom", new { dateFrom = request.FromDate.Value });
                }

                if (request.ToDate.HasValue)
                {
                    builder.Where($"{nameof(Delivery.DateLoadedEnd)} <= @dateTo", new { dateTo = request.ToDate.Value });
                }

                var tmp = builder.AddTemplate(
                    @$"SELECT * FROM {typeof(Delivery).GetTableName()} /**where**/ /**orderby**/"
                );

                var deliveries = uow.Connection.Query<Delivery>(tmp.RawSql, tmp.Parameters);

                var paged = Domain.Models.PagedList<Delivery>.ToPagedList(deliveries, request.Page, request.PageSize, 0);

                return paged;
            }
        }
    }
}
