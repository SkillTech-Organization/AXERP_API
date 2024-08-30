using AXERP.API.Domain;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;

namespace AXERP.API.Business.Commands
{
    [ForSystem("SQL Server", LogConstants.FUNCTION_GOOGLE_SYNC)]
    public class UpdateBillOfLadingCommand : BaseAuditedClass<UpdateBillOfLadingCommand>
    {
        private readonly UnitOfWorkFactory _uowFactory;

        public UpdateBillOfLadingCommand(
            AxerpLoggerFactory axerpLoggerFactory,
            UnitOfWorkFactory uowFactory) : base(axerpLoggerFactory)
        {
            _uowFactory = uowFactory;
        }

        public BaseResponse Execute(string deliveryID)
        {
            var res = new BaseResponse();

            using (var uow = _uowFactory.Create())
            {
                try
                {
                    var delivery = uow.TransactionRepository.GetById(deliveryID);

                    if (delivery == null)
                    {
                        res.HttpStatusCode = System.Net.HttpStatusCode.NotFound;
                        res.RequestError = $"Delivery with id: {deliveryID} cannot not be found.";
                    }
                    else
                    {
                        if (delivery.BillOfLading == null)
                        {
                            delivery.BillOfLading = DateTime.Now;
                            uow.TransactionRepository.Update(delivery);

                            _logger.LogInformation("Bill Of Lading set to: {0}", delivery.BillOfLading);
                        }
                        else
                        {
                            _logger.LogInformation("Bill of Lading is already set to: {0}", delivery.BillOfLading);
                        }
                    }
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
