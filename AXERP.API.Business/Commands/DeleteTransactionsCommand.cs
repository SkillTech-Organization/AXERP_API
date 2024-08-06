using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.Persistence.Factories;
using Microsoft.Extensions.Logging;

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
                return res;
            }

            using (var uow = _uowFactory.Create())
            {
                try
                {
                    uow.BeginTransaction();

                    var customerToDeliveries = uow.CustomerToDeliveryRepository.GetAll().Where(x => request.TransactionIds.Contains(x.DeliveryID));
                    var truckCompanyToDeliveries = uow.TruckCompanyToDeliveryRepository.GetAll().Where(x => request.TransactionIds.Contains(x.DeliveryID));
                    var transactions = uow.TransactionRepository.GetAll().Where(x => request.TransactionIds.Contains(x.ID));

                    //TODO: cascade delete on DB level
                    uow.CustomerToDeliveryRepository.Delete(customerToDeliveries);
                    uow.TruckCompanyToDeliveryRepository.Delete(truckCompanyToDeliveries);
                    uow.TransactionRepository.Delete(transactions);

                    uow.CommitTransaction();
                }
                catch (Exception ex)
                {
                    uow.Rollback();
                    throw;
                }
            }

            return res;
        }
    }
}
