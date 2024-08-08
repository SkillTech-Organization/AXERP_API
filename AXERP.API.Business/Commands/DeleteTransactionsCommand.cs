using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.Persistence.Factories;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AXERP.API.Business.Commands
{
    public class DeleteTransactionsCommand
    {
        private readonly ILogger<DeleteTransactionsCommand> _logger;
        private readonly UnitOfWorkFactory _uowFactory;

        public DeleteTransactionsCommand(
            ILogger<DeleteTransactionsCommand> logger,
            UnitOfWorkFactory uowFactory)
        {
            _logger = logger;
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

            _logger.LogInformation("Deleting transactions with ids: {ids}", string.Join(", ", request.TransactionIds));

            using (var uow = _uowFactory.Create())
            {
                try
                {
                    uow.BeginTransaction();

                    var customerToDeliveries = uow.CustomerToDeliveryRepository.GetAll().Where(x => request.TransactionIds.Contains(x.DeliveryID));
                    var truckCompanyToDeliveries = uow.TruckCompanyToDeliveryRepository.GetAll().Where(x => request.TransactionIds.Contains(x.DeliveryID));
                    var transactions = uow.TransactionRepository.GetAll().Where(x => request.TransactionIds.Contains(x.ID));

                    //TODO: cascade delete on DB level
                    _logger.LogInformation("Deleting {count} CustomerToDelivery records...", customerToDeliveries.Count());
                    uow.CustomerToDeliveryRepository.Delete(customerToDeliveries);

                    _logger.LogInformation("Deleting {count} TruckCompanyToDelivery records...", truckCompanyToDeliveries.Count());
                    uow.TruckCompanyToDeliveryRepository.Delete(truckCompanyToDeliveries);

                    _logger.LogInformation("Deleting {count} Transaction records...", transactions.Count());
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
