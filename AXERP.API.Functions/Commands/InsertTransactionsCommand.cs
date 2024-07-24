using AutoMapper;
using AXERP.API.Business.Factories;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.GoogleHelper.Models;
using Microsoft.Extensions.Logging;

namespace AXERP.API.Business.Commands
{
    public class InsertTransactionsCommand
    {
        private readonly ILogger<InsertTransactionsCommand> _logger;
        private readonly UnitOfWorkFactory _uowFactory;
        private readonly IMapper _mapper;

        public InsertTransactionsCommand(
            ILogger<InsertTransactionsCommand> logger,
            UnitOfWorkFactory uowFactory,
            IMapper mapper)
        {
            _logger = logger;
            _uowFactory = uowFactory;
            _mapper = mapper;
        }

        public ImportGasTransactionResponse Execute(GenericSheetImportResult<GasTransaction> importResult)
        {
            var res = new ImportGasTransactionResponse
            {
                InvalidRows = importResult.InvalidRows,
                NewRowsInsertedIntoDatabase = 0,
                TotalDataRowsInSheet = importResult.TotalRowsInSheet,
                ImportErrors = importResult.Errors
            };

            if (importResult == null || importResult.Data == null)
            {
                throw new Exception("Failed google sheet import.");
            }

            using (var uow = _uowFactory.Create())
            {
                try
                {
                    /*
                     * FILTER NEW DATA
                     */

                    // New Deliveries row check
                    var ids = uow.GenericRepository.GetAllIDs<Delivery, string>();
                    var newSheetRows = importResult.Data.Where(x => !ids.Contains(x.DeliveryID));

                    var locations = uow.LocationRepository.GetAll().ToList();
                    var entities = uow.EntityRepository.GetAll().ToList();
                    var truckCompanies = uow.TruckCompanyRepository.GetAll().ToList();
                    var customers = uow.CustomerRepository.GetAll().ToList();
                    var transporters = uow.TransporterRepository.GetAll().ToList();

                    var ctd = new List<CustomerToDelivery>();
                    var ttd = new List<TruckCompanyToDelivery>();

                    var deliveries = new List<Delivery>();

                    uow.BeginTransaction();

                    foreach (var newSheetRow in newSheetRows)
                    {
                        var delivery = _mapper.Map<Delivery>(newSheetRow);

                        delivery.ID = newSheetRow.DeliveryID;

                        var newTerminal = locations.FirstOrDefault(x => x.Name == newSheetRow.Terminal);
                        if (newTerminal == null && !string.IsNullOrWhiteSpace(newSheetRow.Terminal) && !locations.Any(x => x.Name == newSheetRow.Terminal))
                        {
                            newTerminal = uow.LocationRepository.Add(new { Name = newSheetRow.Terminal });
                            locations.Add(newTerminal);
                        }

                        var newDeliveryPoint = locations.FirstOrDefault(x => x.Name == newSheetRow.DeliveryPoint);
                        if (newDeliveryPoint == null && !string.IsNullOrWhiteSpace(newSheetRow.DeliveryPoint) && !locations.Any(x => x.Name == newSheetRow.DeliveryPoint))
                        {
                            newDeliveryPoint = uow.LocationRepository.Add(new { Name = newSheetRow.DeliveryPoint });
                            locations.Add(newDeliveryPoint);
                        }

                        var newSpecificPoint = locations.FirstOrDefault(x => x.Name == newSheetRow.SpecificDeliveryPoint);
                        if (newSpecificPoint == null && !string.IsNullOrWhiteSpace(newSheetRow.SpecificDeliveryPoint) && !locations.Any(x => x.Name == newSheetRow.SpecificDeliveryPoint))
                        {
                            newSpecificPoint = uow.LocationRepository.Add(new { Name = newSheetRow.SpecificDeliveryPoint });
                            locations.Add(newSpecificPoint);
                        }

                        var newRef = entities.FirstOrDefault(x => x.Name == newSheetRow.Reference);
                        if (newRef == null && !string.IsNullOrWhiteSpace(newSheetRow.Reference) && !entities.Any(x => x.Name == newSheetRow.Reference))
                        {
                            newRef = uow.EntityRepository.Add(new { Name = newSheetRow.Reference });
                            entities.Add(newRef);
                        }

                        var newRef2 = entities.FirstOrDefault(x => x.Name == newSheetRow.Reference2);
                        if (newRef2 == null && !string.IsNullOrWhiteSpace(newSheetRow.Reference2) && !entities.Any(x => x.Name == newSheetRow.Reference2))
                        {
                            newRef2 = uow.EntityRepository.Add(new { Name = newSheetRow.Reference2 });
                            entities.Add(newRef2);
                        }

                        var newRef3 = entities.FirstOrDefault(x => x.Name == newSheetRow.Reference3);
                        if (newRef3 == null && !string.IsNullOrWhiteSpace(newSheetRow.Reference3) && !entities.Any(x => x.Name == newSheetRow.Reference3))
                        {
                            newRef3 = uow.EntityRepository.Add(new { Name = newSheetRow.Reference3 });
                            entities.Add(newRef3);
                        }

                        var newTransporter = transporters.FirstOrDefault(x => x.Name == newSheetRow.Transporter);
                        if (newTransporter == null && !string.IsNullOrWhiteSpace(newSheetRow.Transporter) && !transporters.Any(x => x.Name == newSheetRow.Transporter))
                        {
                            newTransporter = uow.TransporterRepository.Add(new { Name = newSheetRow.Transporter });
                            transporters.Add(newTransporter);
                        }

                        var newTruckCompany = truckCompanies.FirstOrDefault(x => x.Name == newSheetRow.TruckCompany);
                        if (newTruckCompany == null && !string.IsNullOrWhiteSpace(newSheetRow.TruckCompany) && !truckCompanies.Any(x => x.Name == newSheetRow.TruckCompany))
                        {
                            newTruckCompany = uow.TruckCompanyRepository.Add(new { Name = newSheetRow.TruckCompany });
                            truckCompanies.Add(newTruckCompany);
                        }

                        var newCustomer = customers.FirstOrDefault(x => x.Name == newSheetRow.Customer);
                        if (newCustomer == null && !string.IsNullOrWhiteSpace(newSheetRow.Customer) && !customers.Any(x => x.Name == newSheetRow.Customer))
                        {
                            newCustomer = uow.CustomerRepository.Add(new { Name = newSheetRow.Customer });
                            customers.Add(newCustomer);
                        }

                        delivery.TerminalID = newTerminal?.ID;

                        delivery.DeliveryPointID = newDeliveryPoint?.ID;
                        delivery.SpecificDeliveryPointID = newSpecificPoint?.ID;

                        delivery.ReferenceID1 = newRef?.ID;
                        delivery.ReferenceID2 = newRef2?.ID;
                        delivery.ReferenceID3 = newRef3?.ID;

                        delivery.TransporterID = newTransporter?.ID;

                        if (newCustomer != null)
                        {
                            ctd.Add(new CustomerToDelivery
                            {
                                DeliveryID = delivery.ID,
                                CustomerID = newCustomer.ID,
                                Comment = newSheetRow.CustomerNote
                            });
                        }

                        if (newTruckCompany != null)
                        {
                            ttd.Add(new TruckCompanyToDelivery
                            {
                                DeliveryID = delivery.ID,
                                TruckCompanyID = newTruckCompany.ID,
                                Comment = newSheetRow.TruckLoadingCompanyComment
                            });
                        }

                        deliveries.Add(delivery);
                    }

                    uow.Save("pt-before-deliveries");

                    // New Deliveries
                    res.NewRowsInsertedIntoDatabase += uow.DeliveryRepository.Add(deliveries, true);

                    uow.Save("pt-before-ctd-ttd");

                    uow.CustomerToDeliveryRepository.Add(ctd);
                    uow.TruckCompanyToDeliveryRepository.Add(ttd);

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
