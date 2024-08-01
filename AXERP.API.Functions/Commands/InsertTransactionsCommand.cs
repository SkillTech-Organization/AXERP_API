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

        public ImportGasTransactionResponse Execute(GenericSheetImportResult<Delivery> importResult)
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
                var dv = new List<Transaction>();

                try
                {
                    /*
                     * FILTER NEW DATA
                     */

                    // New Deliveries row check
                    var ids = uow.GenericRepository.GetAllIDs<Transaction, string>();
                    var newSheetRows = importResult.Data.Where(x => !ids.Contains(x.DeliveryID));

                    var interfaces = uow.InterfaceRepository.GetAll().ToList();
                    var statuses = uow.TransactionStatusRepository.GetAll().Select(x => x.Name).ToList();
                    var documents = uow.DocumentRepository.GetAll().ToList();
                    var truckCompanies = uow.TruckCompanyRepository.GetAll().ToList();
                    var entities = uow.EntityRepository.GetAll().ToList();

                    var ctd = new List<CustomerToDelivery>();
                    var ttd = new List<TruckCompanyToDelivery>();

                    var transactions = new List<Transaction>();

                    uow.BeginTransaction();

                    foreach (var newSheetRow in newSheetRows)
                    {
                        var transaction = _mapper.Map<Transaction>(newSheetRow);

                        transaction.ID = newSheetRow.DeliveryID;

                        transaction.StatusID = newSheetRow.Status;
                        transaction.SalesStatusID = newSheetRow.SalesStatus;

                        var newTerminal = interfaces.FirstOrDefault(x => x.Name == newSheetRow.Terminal);
                        if (newTerminal == null && !string.IsNullOrWhiteSpace(newSheetRow.Terminal) && !interfaces.Any(x => x.Name == newSheetRow.Terminal))
                        {
                            newTerminal = uow.InterfaceRepository.Add(new Interface { Name = newSheetRow.Terminal });
                            interfaces.Add(newTerminal);
                        }

                        var newDeliveryPoint = interfaces.FirstOrDefault(x => x.Name == newSheetRow.DeliveryPoint);
                        if (newDeliveryPoint == null && !string.IsNullOrWhiteSpace(newSheetRow.DeliveryPoint) && !interfaces.Any(x => x.Name == newSheetRow.DeliveryPoint))
                        {
                            newDeliveryPoint = uow.InterfaceRepository.Add(new Interface { Name = newSheetRow.DeliveryPoint });
                            interfaces.Add(newDeliveryPoint);
                        }

                        var newSpecificPoint = interfaces.FirstOrDefault(x => x.Name == newSheetRow.SpecificDeliveryPoint);
                        if (newSpecificPoint == null && !string.IsNullOrWhiteSpace(newSheetRow.SpecificDeliveryPoint) && !interfaces.Any(x => x.Name == newSheetRow.SpecificDeliveryPoint))
                        {
                            newSpecificPoint = uow.InterfaceRepository.Add(new Interface { Name = newSheetRow.SpecificDeliveryPoint });
                            interfaces.Add(newSpecificPoint);
                        }

                        var newRef = documents.FirstOrDefault(x => x.Name == newSheetRow.Reference);
                        if (newRef == null && !string.IsNullOrWhiteSpace(newSheetRow.Reference) && !documents.Any(x => x.Name == newSheetRow.Reference))
                        {
                            newRef = uow.DocumentRepository.Add(new Document { Name = newSheetRow.Reference });
                            documents.Add(newRef);
                        }

                        var newRef2 = documents.FirstOrDefault(x => x.Name == newSheetRow.Reference2);
                        if (newRef2 == null && !string.IsNullOrWhiteSpace(newSheetRow.Reference2) && !documents.Any(x => x.Name == newSheetRow.Reference2))
                        {
                            newRef2 = uow.DocumentRepository.Add(new Document { Name = newSheetRow.Reference2 });
                            documents.Add(newRef2);
                        }

                        var newRef3 = documents.FirstOrDefault(x => x.Name == newSheetRow.Reference3);
                        if (newRef3 == null && !string.IsNullOrWhiteSpace(newSheetRow.Reference3) && !documents.Any(x => x.Name == newSheetRow.Reference3))
                        {
                            newRef3 = uow.DocumentRepository.Add(new Document { Name = newSheetRow.Reference3 });
                            documents.Add(newRef3);
                        }

                        var newTransporter = entities.FirstOrDefault(x => x.Name == newSheetRow.Transporter);
                        if (newTransporter == null && !string.IsNullOrWhiteSpace(newSheetRow.Transporter) && !entities.Any(x => x.Name == newSheetRow.Transporter))
                        {
                            newTransporter = uow.EntityRepository.Add(new Entity { Name = newSheetRow.Transporter });
                            entities.Add(newTransporter);
                        }

                        var newTruckCompany = truckCompanies.FirstOrDefault(x => x.Name == newSheetRow.TruckCompany);
                        if (newTruckCompany == null && !string.IsNullOrWhiteSpace(newSheetRow.TruckCompany) && !truckCompanies.Any(x => x.Name == newSheetRow.TruckCompany))
                        {
                            newTruckCompany = uow.TruckCompanyRepository.Add(new Entity { Name = newSheetRow.TruckCompany });
                            truckCompanies.Add(newTruckCompany);
                        }

                        var newCustomer = entities.FirstOrDefault(x => x.Name == newSheetRow.Customer);
                        if (newCustomer == null && !string.IsNullOrWhiteSpace(newSheetRow.Customer) && !entities.Any(x => x.Name == newSheetRow.Customer))
                        {
                            newCustomer = uow.EntityRepository.Add(new Entity { Name = newSheetRow.Customer });
                            entities.Add(newCustomer);
                        }

                        transaction.TerminalID = newTerminal?.ID;

                        transaction.DeliveryPointID = newDeliveryPoint?.ID;
                        transaction.SpecificDeliveryPointID = newSpecificPoint?.ID;

                        transaction.BlFileID = newRef?.ID ?? newRef2?.ID ?? newRef3?.ID;

                        transaction.TransporterID = newTransporter?.ID;

                        if (newCustomer != null)
                        {
                            ctd.Add(new CustomerToDelivery
                            {
                                DeliveryID = transaction.ID,
                                CustomerID = newCustomer.ID,
                                Comment = newSheetRow.CustomerNote
                            });
                        }

                        if (newTruckCompany != null)
                        {
                            ttd.Add(new TruckCompanyToDelivery
                            {
                                DeliveryID = transaction.ID,
                                TruckCompanyID = newTruckCompany.ID,
                                Comment = newSheetRow.TruckLoadingCompanyComment
                            });
                        }

                        transactions.Add(transaction);
                    }

                    uow.Save("pt-before-deliveries");

                    // New Deliveries
                    dv = transactions.ToList();
                    res.NewRowsInsertedIntoDatabase += uow.TransactionRepository.Add(transactions, true);

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
