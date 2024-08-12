using AutoMapper;
using AXERP.API.Persistence.Factories;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.GoogleHelper.Models;
using Microsoft.Extensions.Logging;
using AXERP.API.Domain.Interfaces.UnitOfWork;

namespace AXERP.API.Business.Commands
{
    public class InsertTransactionsCommand
    {
        private readonly ILogger<InsertTransactionsCommand> _logger;
        private readonly UnitOfWorkFactory _uowFactory;
        private readonly IMapper _mapper;

        private IEnumerable<Transaction> Transactions { get; set; }
        private IEnumerable<string> TransactionIds { get; set; }
        private IEnumerable<Interface> Interfaces { get; set; }
        private IEnumerable<string> Statuses { get; set; }
        private IEnumerable<Document> Documents { get; set; }
        private IEnumerable<TruckCompany> TruckCompanies { get; set; }
        private IEnumerable<Entity> Entities { get; set; }

        public InsertTransactionsCommand(
            ILogger<InsertTransactionsCommand> logger,
            UnitOfWorkFactory uowFactory,
            IMapper mapper)
        {
            _logger = logger;
            _uowFactory = uowFactory;
            _mapper = mapper;
        }

        private void PrepareData(IUnitOfWork uow)
        {
            Transactions = uow.TransactionRepository.GetAll();
            TransactionIds = Transactions.Select(x => x.ID);
            Interfaces = uow.InterfaceRepository.GetAll();
            Statuses = uow.TransactionStatusRepository.GetAll().Select(x => x.Name);
            Documents = uow.DocumentRepository.GetAll();
            TruckCompanies = uow.TruckCompanyRepository.GetAll();
            Entities = uow.EntityRepository.GetAll();
        }

        private void Delete(IUnitOfWork uow, IEnumerable<string> ids)
        {
            uow.TransactionRepository.Delete(ids);
            uow.Save("delete_done");
        }

        public ImportGasTransactionResponse Execute(GenericSheetImportResult<Delivery> importResult)
        {
            var res = new ImportGasTransactionResponse
            {
                InvalidRows = importResult.InvalidRows,
                NewRows = 0,
                UpdatedRows = 0,
                DeletedRows = 0,
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
                     * FILTER NEW / UPDATED / DELETED DATA
                     */

                    // New Deliveries row check
                    var dbTransactions = uow.TransactionRepository.GetAll();
                    var ids = dbTransactions.Select(x => x.ID);

                    var sheetIds = importResult.Data.Select(x => x.DeliveryID);

                    var newSheetRows = importResult.Data.Where(x => !ids.Contains(x.DeliveryID));
                    var updatedSheetRows = importResult.Data.Where(x => dbTransactions.Any(y => x.DeliveryID == y.ID && x.AXERPHash != y.AXERPHash));
                    var deletedSheetRowIds = ids.Where(x => !sheetIds.Contains(x));

                    var interfaces = uow.InterfaceRepository.GetAll().ToList();
                    var statuses = uow.TransactionStatusRepository.GetAll().Select(x => x.Name).ToList();
                    var documents = uow.DocumentRepository.GetAll().ToList();
                    var truckCompanies = uow.TruckCompanyRepository.GetAll().ToList();
                    var entities = uow.EntityRepository.GetAll().ToList();

                    var ctd = new List<CustomerToDelivery>();
                    var ttd = new List<TruckCompanyToDelivery>();

                    var newTransactions = new List<Transaction>();
                    var updatedTransactions = new List<Transaction>();

                    res.NewRows = newSheetRows.Count();
                    res.UpdatedRows = updatedSheetRows.Count();
                    res.DeletedRows = deletedSheetRowIds.Count();

                    uow.BeginTransaction();

                    /*
                     * DELETE TRANSACTIONS
                     */

                    uow.TransactionRepository.Delete(deletedSheetRowIds);

                    uow.Save("delete_done");

                    /*
                     * UPDATE TRANSACTIONS
                     */

                    foreach (var sheetRow in updatedSheetRows)
                    {
                        var transaction = _mapper.Map<Transaction>(sheetRow);

                        transaction.ID = sheetRow.DeliveryID;

                        transaction.StatusID = sheetRow.Status;
                        transaction.SalesStatusID = sheetRow.SalesStatus;

                        var newTerminal = interfaces.FirstOrDefault(x => x.Name == sheetRow.Terminal);
                        if (newTerminal == null && !string.IsNullOrWhiteSpace(sheetRow.Terminal) && !interfaces.Any(x => x.Name == sheetRow.Terminal))
                        {
                            newTerminal = uow.InterfaceRepository.Add(new Interface { Name = sheetRow.Terminal });
                            interfaces.Add(newTerminal);
                        }

                        var newDeliveryPoint = interfaces.FirstOrDefault(x => x.Name == sheetRow.DeliveryPoint);
                        if (newDeliveryPoint == null && !string.IsNullOrWhiteSpace(sheetRow.DeliveryPoint) && !interfaces.Any(x => x.Name == sheetRow.DeliveryPoint))
                        {
                            newDeliveryPoint = uow.InterfaceRepository.Add(new Interface { Name = sheetRow.DeliveryPoint });
                            interfaces.Add(newDeliveryPoint);
                        }

                        var newSpecificPoint = interfaces.FirstOrDefault(x => x.Name == sheetRow.SpecificDeliveryPoint);
                        if (newSpecificPoint == null && !string.IsNullOrWhiteSpace(sheetRow.SpecificDeliveryPoint) && !interfaces.Any(x => x.Name == sheetRow.SpecificDeliveryPoint))
                        {
                            newSpecificPoint = uow.InterfaceRepository.Add(new Interface { Name = sheetRow.SpecificDeliveryPoint });
                            interfaces.Add(newSpecificPoint);
                        }

                        var newRef3 = documents.FirstOrDefault(x => x.Name == sheetRow.Reference3);
                        if (newRef3 == null && !string.IsNullOrWhiteSpace(sheetRow.Reference3) && !documents.Any(x => x.Name == sheetRow.Reference3))
                        {
                            newRef3 = uow.DocumentRepository.Add(new Document { Name = sheetRow.Reference3 });
                            documents.Add(newRef3);
                            transaction.BlFileID = newRef3.ID;
                        }

                        var newRef2 = documents.FirstOrDefault(x => x.Name == sheetRow.Reference2);
                        if (newRef2 == null && !string.IsNullOrWhiteSpace(sheetRow.Reference2) && !documents.Any(x => x.Name == sheetRow.Reference2))
                        {
                            newRef2 = uow.DocumentRepository.Add(new Document { Name = sheetRow.Reference2 });
                            documents.Add(newRef2);
                            transaction.BlFileID = newRef2.ID;
                        }

                        var newRef = documents.FirstOrDefault(x => x.Name == sheetRow.Reference);
                        if (newRef == null && !string.IsNullOrWhiteSpace(sheetRow.Reference) && !documents.Any(x => x.Name == sheetRow.Reference))
                        {
                            newRef = uow.DocumentRepository.Add(new Document { Name = sheetRow.Reference });
                            documents.Add(newRef);
                            transaction.BlFileID = newRef.ID;
                        }

                        var newTransporter = entities.FirstOrDefault(x => x.Name == sheetRow.Transporter);
                        if (newTransporter == null && !string.IsNullOrWhiteSpace(sheetRow.Transporter) && !entities.Any(x => x.Name == sheetRow.Transporter))
                        {
                            newTransporter = uow.EntityRepository.Add(new Entity { Name = sheetRow.Transporter });
                            entities.Add(newTransporter);
                        }

                        var newTruckCompany = truckCompanies.FirstOrDefault(x => x.Name == sheetRow.TruckCompany);
                        if (newTruckCompany == null && !string.IsNullOrWhiteSpace(sheetRow.TruckCompany) && !truckCompanies.Any(x => x.Name == sheetRow.TruckCompany))
                        {
                            newTruckCompany = uow.TruckCompanyRepository.Add(new Entity { Name = sheetRow.TruckCompany });
                            truckCompanies.Add(newTruckCompany);
                        }

                        var newCustomer = entities.FirstOrDefault(x => x.Name == sheetRow.Customer);
                        if (newCustomer == null && !string.IsNullOrWhiteSpace(sheetRow.Customer) && !entities.Any(x => x.Name == sheetRow.Customer))
                        {
                            newCustomer = uow.EntityRepository.Add(new Entity { Name = sheetRow.Customer });
                            entities.Add(newCustomer);
                        }

                        transaction.TerminalID = newTerminal?.ID;

                        transaction.DeliveryPointID = newDeliveryPoint?.ID;
                        transaction.SpecificDeliveryPointID = newSpecificPoint?.ID;

                        transaction.TransporterID = newTransporter?.ID;

                        if (newCustomer != null)
                        {
                            ctd.Add(new CustomerToDelivery
                            {
                                DeliveryID = transaction.ID,
                                CustomerID = newCustomer.ID,
                                Comment = sheetRow.CustomerNote
                            });
                        }

                        if (newTruckCompany != null)
                        {
                            ttd.Add(new TruckCompanyToDelivery
                            {
                                DeliveryID = transaction.ID,
                                TruckCompanyID = newTruckCompany.ID,
                                Comment = sheetRow.TruckLoadingCompanyComment
                            });
                        }

                        updatedTransactions.Add(transaction);
                    }

                    uow.TransactionRepository.Update(updatedTransactions);

                    uow.CustomerToDeliveryRepository.Add(ctd);
                    uow.TruckCompanyToDeliveryRepository.Add(ttd);

                    ctd.Clear();
                    ttd.Clear();

                    uow.Save("update_done");

                    /*
                     * CREATE TRANSACTIONS
                     */

                    foreach (var sheetRow in newSheetRows)
                    {
                        var transaction = _mapper.Map<Transaction>(sheetRow);

                        transaction.ID = sheetRow.DeliveryID;

                        transaction.StatusID = sheetRow.Status;
                        transaction.SalesStatusID = sheetRow.SalesStatus;

                        var newTerminal = interfaces.FirstOrDefault(x => x.Name == sheetRow.Terminal);
                        if (newTerminal == null && !string.IsNullOrWhiteSpace(sheetRow.Terminal) && !interfaces.Any(x => x.Name == sheetRow.Terminal))
                        {
                            newTerminal = uow.InterfaceRepository.Add(new Interface { Name = sheetRow.Terminal });
                            interfaces.Add(newTerminal);
                        }

                        var newDeliveryPoint = interfaces.FirstOrDefault(x => x.Name == sheetRow.DeliveryPoint);
                        if (newDeliveryPoint == null && !string.IsNullOrWhiteSpace(sheetRow.DeliveryPoint) && !interfaces.Any(x => x.Name == sheetRow.DeliveryPoint))
                        {
                            newDeliveryPoint = uow.InterfaceRepository.Add(new Interface { Name = sheetRow.DeliveryPoint });
                            interfaces.Add(newDeliveryPoint);
                        }

                        var newSpecificPoint = interfaces.FirstOrDefault(x => x.Name == sheetRow.SpecificDeliveryPoint);
                        if (newSpecificPoint == null && !string.IsNullOrWhiteSpace(sheetRow.SpecificDeliveryPoint) && !interfaces.Any(x => x.Name == sheetRow.SpecificDeliveryPoint))
                        {
                            newSpecificPoint = uow.InterfaceRepository.Add(new Interface { Name = sheetRow.SpecificDeliveryPoint });
                            interfaces.Add(newSpecificPoint);
                        }

                        var newRef3 = documents.FirstOrDefault(x => x.Name == sheetRow.Reference3);
                        if (newRef3 == null && !string.IsNullOrWhiteSpace(sheetRow.Reference3) && !documents.Any(x => x.Name == sheetRow.Reference3))
                        {
                            newRef3 = uow.DocumentRepository.Add(new Document { Name = sheetRow.Reference3 });
                            documents.Add(newRef3);
                            transaction.BlFileID = newRef3.ID;
                        }

                        var newRef2 = documents.FirstOrDefault(x => x.Name == sheetRow.Reference2);
                        if (newRef2 == null && !string.IsNullOrWhiteSpace(sheetRow.Reference2) && !documents.Any(x => x.Name == sheetRow.Reference2))
                        {
                            newRef2 = uow.DocumentRepository.Add(new Document { Name = sheetRow.Reference2 });
                            documents.Add(newRef2);
                            transaction.BlFileID = newRef2.ID;
                        }

                        var newRef = documents.FirstOrDefault(x => x.Name == sheetRow.Reference);
                        if (newRef == null && !string.IsNullOrWhiteSpace(sheetRow.Reference) && !documents.Any(x => x.Name == sheetRow.Reference))
                        {
                            newRef = uow.DocumentRepository.Add(new Document { Name = sheetRow.Reference });
                            documents.Add(newRef);
                            transaction.BlFileID = newRef.ID;
                        }

                        var newTransporter = entities.FirstOrDefault(x => x.Name == sheetRow.Transporter);
                        if (newTransporter == null && !string.IsNullOrWhiteSpace(sheetRow.Transporter) && !entities.Any(x => x.Name == sheetRow.Transporter))
                        {
                            newTransporter = uow.EntityRepository.Add(new Entity { Name = sheetRow.Transporter });
                            entities.Add(newTransporter);
                        }

                        var newTruckCompany = truckCompanies.FirstOrDefault(x => x.Name == sheetRow.TruckCompany);
                        if (newTruckCompany == null && !string.IsNullOrWhiteSpace(sheetRow.TruckCompany) && !truckCompanies.Any(x => x.Name == sheetRow.TruckCompany))
                        {
                            newTruckCompany = uow.TruckCompanyRepository.Add(new Entity { Name = sheetRow.TruckCompany });
                            truckCompanies.Add(newTruckCompany);
                        }

                        var newCustomer = entities.FirstOrDefault(x => x.Name == sheetRow.Customer);
                        if (newCustomer == null && !string.IsNullOrWhiteSpace(sheetRow.Customer) && !entities.Any(x => x.Name == sheetRow.Customer))
                        {
                            newCustomer = uow.EntityRepository.Add(new Entity { Name = sheetRow.Customer });
                            entities.Add(newCustomer);
                        }

                        transaction.TerminalID = newTerminal?.ID;

                        transaction.DeliveryPointID = newDeliveryPoint?.ID;
                        transaction.SpecificDeliveryPointID = newSpecificPoint?.ID;

                        transaction.TransporterID = newTransporter?.ID;

                        if (newCustomer != null)
                        {
                            ctd.Add(new CustomerToDelivery
                            {
                                DeliveryID = transaction.ID,
                                CustomerID = newCustomer.ID,
                                Comment = sheetRow.CustomerNote
                            });
                        }

                        if (newTruckCompany != null)
                        {
                            ttd.Add(new TruckCompanyToDelivery
                            {
                                DeliveryID = transaction.ID,
                                TruckCompanyID = newTruckCompany.ID,
                                Comment = sheetRow.TruckLoadingCompanyComment
                            });
                        }

                        newTransactions.Add(transaction);
                    }

                    uow.TransactionRepository.Add(newTransactions, true);

                    uow.CustomerToDeliveryRepository.Add(ctd);
                    uow.TruckCompanyToDeliveryRepository.Add(ttd);

                    uow.Save("create_done");

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
