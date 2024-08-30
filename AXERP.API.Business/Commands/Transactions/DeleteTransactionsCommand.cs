using AXERP.API.LogHelper.Base;
using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;
using AXERP.API.Domain;

namespace AXERP.API.Business.Commands
{
    [ForSystem("SQL Server", LogConstants.FUNCTION_GOOGLE_SYNC)]
    public class DeleteTransactionsCommand : BaseAuditedClass<DeleteTransactionsCommand>
    {
        private readonly UnitOfWorkFactory _uowFactory;

        public DeleteTransactionsCommand(
            AxerpLoggerFactory axerpLoggerFactory,
            UnitOfWorkFactory uowFactory) : base(axerpLoggerFactory)
        {
            _uowFactory = uowFactory;
        }

        public BaseResponse Execute(DeleteTransactionRequest request)
        {
            var res = new BaseResponse();

            if (!request.TransactionIds.Any())
            {
                _logger.LogWarning("Transactions cannot be deleted, an empty list of ids were provided!");
                return res;
            }

            _logger.LogInformation("Deleting transactions with ids: {0}", string.Join(", ", request.TransactionIds));

            using (var uow = _uowFactory.Create())
            {
                try
                {
                    uow.BeginTransaction();

                    var customerToDeliveries = uow.CustomerToDeliveryRepository.GetAll().Where(x => request.TransactionIds.Contains($"{x.DeliveryID}{x.DeliveryIDSffx}"));
                    var truckCompanyToDeliveries = uow.TruckCompanyToDeliveryRepository.GetAll().Where(x => request.TransactionIds.Contains($"{x.DeliveryID}{x.DeliveryIDSffx}"));
                    var transactions = uow.TransactionRepository.GetAll().Where(x => request.TransactionIds.Contains($"{x.ID}{x.IDSffx}"));

                    _logger.LogInformation("Deleting {0} CustomerToDelivery records...", customerToDeliveries.Count());
                    uow.CustomerToDeliveryRepository.Delete(customerToDeliveries);

                    _logger.LogInformation("Deleting {0} TruckCompanyToDelivery records...", truckCompanyToDeliveries.Count());
                    uow.TruckCompanyToDeliveryRepository.Delete(truckCompanyToDeliveries);

                    _logger.LogInformation("Deleting {0} Transaction records...", transactions.Count());
                    uow.TransactionRepository.Delete(transactions);

                    uow.CommitTransaction();
                }
                catch (Exception ex)
                {
                    uow.Rollback();
                    throw;
                }
            }

            _logger.LogInformation("Transactions were successfully deleted!");

            return res;
        }
    }
}
