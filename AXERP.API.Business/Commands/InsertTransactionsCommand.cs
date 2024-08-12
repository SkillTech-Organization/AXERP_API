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

        private List<Transaction> Transactions { get; set; }
        private List<string> TransactionIds { get; set; }
        private List<Interface> Interfaces { get; set; }
        private List<string> Statuses { get; set; }
        private List<Document> Documents { get; set; }
        private List<TruckCompany> TruckCompanies { get; set; }
        private List<Entity> Entities { get; set; }
        private List<TruckCompanyToDelivery> TruckCompanyToDeliveries { get; set; }
        private List<CustomerToDelivery> CustomerToDeliveries { get; set; }

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
                     * LOCAL "CACHE"
                     */
                    _logger.LogInformation("Querying data for processing. Transactions, entities...");

                    RefreshBusinessDataCache(uow);

                    /*
                     * FILTER NEW / UPDATED / DELETED DATA
                     */
                    var sheetIds = importResult.Data.Select(x => x.DeliveryID);

                    _logger.LogInformation("Selecting and counting imported rows for CREATE, UPDATE and DELETE.");

                    var newSheetRows = importResult.Data.Where(x => !TransactionIds.Contains(x.DeliveryID));
                    var updatedSheetRows = importResult.Data.Where(x => Transactions.Any(y => x.DeliveryID == y.ID && x.AXERPHash != y.AXERPHash));
                    var deletedSheetRowIds = TransactionIds.Where(x => !sheetIds.Contains(x));

                    res.NewRows = newSheetRows.Count();
                    res.UpdatedRows = updatedSheetRows.Count();
                    res.DeletedRows = deletedSheetRowIds.Count();

                    _logger.LogInformation(
                        "All imported rows: {imported}, invalid rows: {invalid}",
                        importResult.ImportedRowCount,
                        importResult.InvalidRows
                    );

                    _logger.LogInformation(
                        "New rows: {new}, updated rows: {updated}, deleted rows: {deleted}",
                        res.NewRows,
                        res.UpdatedRows,
                        res.DeletedRows
                    );

                    uow.BeginTransaction();

                    /*
                     * DELETE TRANSACTIONS
                     */
                    _logger.LogInformation("Deleting transactions...");

                    Delete(uow, deletedSheetRowIds);


                    _logger.LogInformation("Refreshing data for processing. Transactions, entities...");
                    RefreshBusinessDataCache(uow);

                    /*
                     * UPDATE TRANSACTIONS
                     */
                    _logger.LogInformation("Updating transactions...");

                    Update(uow, updatedSheetRows);


                    _logger.LogInformation("Refreshing data for processing. Transactions, entities...");
                    RefreshBusinessDataCache(uow);

                    /*
                     * CREATE TRANSACTIONS
                     */
                    _logger.LogInformation("Creating transactions...");

                    Create(uow, newSheetRows);

                    uow.CommitTransaction();

                    _logger.LogInformation("Sync (DataBase part) finished without errors.");
                }
                catch (Exception ex)
                {
                    uow.Rollback();
                    throw;
                }
            }

            return res;
        }

        private void RefreshBusinessDataCache(IUnitOfWork uow)
        {
            Transactions = uow.TransactionRepository.GetAll().ToList();
            TransactionIds = Transactions.Select(x => x.ID).ToList();
            Interfaces = uow.InterfaceRepository.GetAll().ToList();
            Statuses = uow.TransactionStatusRepository.GetAll().Select(x => x.Name).ToList();
            Documents = uow.DocumentRepository.GetAll().ToList();
            TruckCompanies = uow.TruckCompanyRepository.GetAll().ToList();
            Entities = uow.EntityRepository.GetAll().ToList();
            TruckCompanyToDeliveries = uow.TruckCompanyToDeliveryRepository.GetAll().ToList();
            CustomerToDeliveries = uow.CustomerToDeliveryRepository.GetAll().ToList();
        }

        private void Delete(IUnitOfWork uow, IEnumerable<string> ids)
        {
            _logger.LogInformation("Deleting associated {name} rows.", nameof(CustomerToDelivery));
            var deleted = uow.CustomerToDeliveryRepository.Delete(nameof(CustomerToDelivery.DeliveryID), ids);
            _logger.LogInformation("Deleted {name} rows: {count}", nameof(CustomerToDelivery), deleted);

            _logger.LogInformation("Deleting associated {name} rows.", nameof(TruckCompanyToDelivery));
            deleted = uow.TruckCompanyToDeliveryRepository.Delete(nameof(TruckCompanyToDelivery.DeliveryID), ids);
            _logger.LogInformation("Deleted {name} rows: {count}", nameof(TruckCompanyToDelivery), deleted);

            _logger.LogInformation("Deleting {name} rows.", nameof(Transaction));
            deleted = uow.TransactionRepository.Delete(ids);
            _logger.LogInformation("Deleted {name} rows: {count}", nameof(Transaction), deleted);

            uow.Save("delete_done");
        }

        private void Update(IUnitOfWork uow, IEnumerable<Delivery> sheetRows)
        {
            if (!sheetRows.Any())
            {
                return;
            }

            var transactionDtos = new List<Transaction>();
            var ctdNew = new List<CustomerToDelivery>();
            var ttdNew = new List<TruckCompanyToDelivery>();
            var ctdUpdate = new List<CustomerToDelivery>();
            var ttdUpdate = new List<TruckCompanyToDelivery>();

            foreach (var sheetRow in sheetRows)
            {
                var transaction = _mapper.Map<Transaction>(sheetRow);

                transaction.ID = sheetRow.DeliveryID;

                transaction.StatusID = sheetRow.Status;
                transaction.SalesStatusID = sheetRow.SalesStatus;

                var newTerminal = Interfaces.FirstOrDefault(x => x.Name == sheetRow.Terminal);
                if (newTerminal == null && !string.IsNullOrWhiteSpace(sheetRow.Terminal) && !Interfaces.Any(x => x.Name == sheetRow.Terminal))
                {
                    newTerminal = uow.InterfaceRepository.Add(new Interface { Name = sheetRow.Terminal });
                    Interfaces.Add(newTerminal);
                }

                var newDeliveryPoint = Interfaces.FirstOrDefault(x => x.Name == sheetRow.DeliveryPoint);
                if (newDeliveryPoint == null && !string.IsNullOrWhiteSpace(sheetRow.DeliveryPoint) && !Interfaces.Any(x => x.Name == sheetRow.DeliveryPoint))
                {
                    newDeliveryPoint = uow.InterfaceRepository.Add(new Interface { Name = sheetRow.DeliveryPoint });
                    Interfaces.Add(newDeliveryPoint);
                }

                var newSpecificPoint = Interfaces.FirstOrDefault(x => x.Name == sheetRow.SpecificDeliveryPoint);
                if (newSpecificPoint == null && !string.IsNullOrWhiteSpace(sheetRow.SpecificDeliveryPoint) && !Interfaces.Any(x => x.Name == sheetRow.SpecificDeliveryPoint))
                {
                    newSpecificPoint = uow.InterfaceRepository.Add(new Interface { Name = sheetRow.SpecificDeliveryPoint });
                    Interfaces.Add(newSpecificPoint);
                }

                var newRef3 = Documents.FirstOrDefault(x => x.Name == sheetRow.Reference3);
                if (newRef3 == null && !string.IsNullOrWhiteSpace(sheetRow.Reference3) && !Documents.Any(x => x.Name == sheetRow.Reference3))
                {
                    newRef3 = uow.DocumentRepository.Add(new Document { Name = sheetRow.Reference3 });
                    Documents.Add(newRef3);
                    transaction.BlFileID = newRef3.ID;
                }

                var newRef2 = Documents.FirstOrDefault(x => x.Name == sheetRow.Reference2);
                if (newRef2 == null && !string.IsNullOrWhiteSpace(sheetRow.Reference2) && !Documents.Any(x => x.Name == sheetRow.Reference2))
                {
                    newRef2 = uow.DocumentRepository.Add(new Document { Name = sheetRow.Reference2 });
                    Documents.Add(newRef2);
                    transaction.BlFileID = newRef2.ID;
                }

                var newRef = Documents.FirstOrDefault(x => x.Name == sheetRow.Reference);
                if (newRef == null && !string.IsNullOrWhiteSpace(sheetRow.Reference) && !Documents.Any(x => x.Name == sheetRow.Reference))
                {
                    newRef = uow.DocumentRepository.Add(new Document { Name = sheetRow.Reference });
                    Documents.Add(newRef);
                    transaction.BlFileID = newRef.ID;
                }

                var newTransporter = Entities.FirstOrDefault(x => x.Name == sheetRow.Transporter);
                if (newTransporter == null && !string.IsNullOrWhiteSpace(sheetRow.Transporter) && !Entities.Any(x => x.Name == sheetRow.Transporter))
                {
                    newTransporter = uow.EntityRepository.Add(new Entity { Name = sheetRow.Transporter });
                    Entities.Add(newTransporter);
                }

                var isTruckCompanyNew = false;
                var sheetTruckCompany = TruckCompanies.FirstOrDefault(x => x.Name == sheetRow.TruckCompany);
                if (sheetTruckCompany == null && !string.IsNullOrWhiteSpace(sheetRow.TruckCompany) && !TruckCompanies.Any(x => x.Name == sheetRow.TruckCompany))
                {
                    sheetTruckCompany = uow.TruckCompanyRepository.Add(new Entity { Name = sheetRow.TruckCompany });
                    TruckCompanies.Add(sheetTruckCompany);
                    isTruckCompanyNew = true;
                }

                var isCustomerNew = false;
                var sheetCustomer = Entities.FirstOrDefault(x => x.Name == sheetRow.Customer);
                if (sheetCustomer == null && !string.IsNullOrWhiteSpace(sheetRow.Customer) && !Entities.Any(x => x.Name == sheetRow.Customer))
                {
                    sheetCustomer = uow.EntityRepository.Add(new Entity { Name = sheetRow.Customer });
                    Entities.Add(sheetCustomer);
                    isCustomerNew = true;
                }

                transaction.TerminalID = newTerminal?.ID;

                transaction.DeliveryPointID = newDeliveryPoint?.ID;
                transaction.SpecificDeliveryPointID = newSpecificPoint?.ID;

                transaction.TransporterID = newTransporter?.ID;

                if (sheetCustomer != null)
                {
                    if (!isCustomerNew)
                    {
                        var u = CustomerToDeliveries.FirstOrDefault(x => x.DeliveryID == sheetRow.DeliveryID);
                        if (u != null)
                        {
                            u.Comment = sheetRow.CustomerNote;
                            ctdUpdate.Add(u);
                        }
                        else
                        {
                            ctdNew.Add(new CustomerToDelivery
                            {
                                DeliveryID = transaction.ID,
                                CustomerID = sheetCustomer.ID,
                                Comment = sheetRow.CustomerNote
                            });
                        }
                    }
                    else
                    {
                        ctdNew.Add(new CustomerToDelivery
                        {
                            DeliveryID = transaction.ID,
                            CustomerID = sheetCustomer.ID,
                            Comment = sheetRow.CustomerNote
                        });
                    }
                }

                if (sheetTruckCompany != null)
                {
                    if (!isTruckCompanyNew)
                    {
                        var u = TruckCompanyToDeliveries.FirstOrDefault(x => x.DeliveryID == sheetRow.DeliveryID);
                        if (u != null)
                        {
                            u.Comment = sheetRow.TruckLoadingCompanyComment;
                            ttdUpdate.Add(u);
                        }
                        else
                        {
                            ttdNew.Add(new TruckCompanyToDelivery
                            {
                                DeliveryID = transaction.ID,
                                TruckCompanyID = sheetTruckCompany.ID,
                                Comment = sheetRow.TruckLoadingCompanyComment
                            });
                        }
                    }
                    else
                    {
                        ttdNew.Add(new TruckCompanyToDelivery
                        {
                            DeliveryID = transaction.ID,
                            TruckCompanyID = sheetTruckCompany.ID,
                            Comment = sheetRow.TruckLoadingCompanyComment
                        });
                    }
                }

                transactionDtos.Add(transaction);
            }

            _logger.LogInformation("Updating {name} rows. Count: {count}", nameof(Transaction), transactionDtos.Count);
            uow.TransactionRepository.Update(transactionDtos);

            _logger.LogInformation("Inserting new {name} rows. Count: {count}", nameof(CustomerToDelivery), ctdNew.Count);
            uow.CustomerToDeliveryRepository.Add(ctdNew);

            _logger.LogInformation("Inserting new {name} rows. Count: {count}", nameof(TruckCompanyToDelivery), ttdNew.Count);
            uow.TruckCompanyToDeliveryRepository.Add(ttdNew);

            _logger.LogInformation("Updating {name} rows. Count: {count}", nameof(CustomerToDelivery), ctdUpdate.Count);
            uow.CustomerToDeliveryRepository.Update(ctdUpdate);

            _logger.LogInformation("Updating {name} rows. Count: {count}", nameof(TruckCompanyToDelivery), ttdUpdate.Count);
            uow.TruckCompanyToDeliveryRepository.Update(ttdUpdate);

            uow.Save("update_done");
        }

        private void Create(IUnitOfWork uow, IEnumerable<Delivery> sheetRows)
        {
            if (!sheetRows.Any())
            {
                return;
            }

            var transactionDtos = new List<Transaction>();
            var ctdNew = new List<CustomerToDelivery>();
            var ttdNew = new List<TruckCompanyToDelivery>();
            var ctdUpdate = new List<CustomerToDelivery>();
            var ttdUpdate = new List<TruckCompanyToDelivery>();

            foreach (var sheetRow in sheetRows)
            {
                var transaction = _mapper.Map<Transaction>(sheetRow);

                transaction.ID = sheetRow.DeliveryID;

                transaction.StatusID = sheetRow.Status;
                transaction.SalesStatusID = sheetRow.SalesStatus;

                var newTerminal = Interfaces.FirstOrDefault(x => x.Name == sheetRow.Terminal);
                if (newTerminal == null && !string.IsNullOrWhiteSpace(sheetRow.Terminal) && !Interfaces.Any(x => x.Name == sheetRow.Terminal))
                {
                    newTerminal = uow.InterfaceRepository.Add(new Interface { Name = sheetRow.Terminal });
                    Interfaces.Add(newTerminal);
                }

                var newDeliveryPoint = Interfaces.FirstOrDefault(x => x.Name == sheetRow.DeliveryPoint);
                if (newDeliveryPoint == null && !string.IsNullOrWhiteSpace(sheetRow.DeliveryPoint) && !Interfaces.Any(x => x.Name == sheetRow.DeliveryPoint))
                {
                    newDeliveryPoint = uow.InterfaceRepository.Add(new Interface { Name = sheetRow.DeliveryPoint });
                    Interfaces.Add(newDeliveryPoint);
                }

                var newSpecificPoint = Interfaces.FirstOrDefault(x => x.Name == sheetRow.SpecificDeliveryPoint);
                if (newSpecificPoint == null && !string.IsNullOrWhiteSpace(sheetRow.SpecificDeliveryPoint) && !Interfaces.Any(x => x.Name == sheetRow.SpecificDeliveryPoint))
                {
                    newSpecificPoint = uow.InterfaceRepository.Add(new Interface { Name = sheetRow.SpecificDeliveryPoint });
                    Interfaces.Add(newSpecificPoint);
                }

                var newRef3 = Documents.FirstOrDefault(x => x.Name == sheetRow.Reference3);
                if (newRef3 == null && !string.IsNullOrWhiteSpace(sheetRow.Reference3) && !Documents.Any(x => x.Name == sheetRow.Reference3))
                {
                    newRef3 = uow.DocumentRepository.Add(new Document { Name = sheetRow.Reference3 });
                    Documents.Add(newRef3);
                    transaction.BlFileID = newRef3.ID;
                }

                var newRef2 = Documents.FirstOrDefault(x => x.Name == sheetRow.Reference2);
                if (newRef2 == null && !string.IsNullOrWhiteSpace(sheetRow.Reference2) && !Documents.Any(x => x.Name == sheetRow.Reference2))
                {
                    newRef2 = uow.DocumentRepository.Add(new Document { Name = sheetRow.Reference2 });
                    Documents.Add(newRef2);
                    transaction.BlFileID = newRef2.ID;
                }

                var newRef = Documents.FirstOrDefault(x => x.Name == sheetRow.Reference);
                if (newRef == null && !string.IsNullOrWhiteSpace(sheetRow.Reference) && !Documents.Any(x => x.Name == sheetRow.Reference))
                {
                    newRef = uow.DocumentRepository.Add(new Document { Name = sheetRow.Reference });
                    Documents.Add(newRef);
                    transaction.BlFileID = newRef.ID;
                }

                var newTransporter = Entities.FirstOrDefault(x => x.Name == sheetRow.Transporter);
                if (newTransporter == null && !string.IsNullOrWhiteSpace(sheetRow.Transporter) && !Entities.Any(x => x.Name == sheetRow.Transporter))
                {
                    newTransporter = uow.EntityRepository.Add(new Entity { Name = sheetRow.Transporter });
                    Entities.Add(newTransporter);
                }

                var isTruckCompanyNew = false;
                var sheetTruckCompany = TruckCompanies.FirstOrDefault(x => x.Name == sheetRow.TruckCompany);
                if (sheetTruckCompany == null && !string.IsNullOrWhiteSpace(sheetRow.TruckCompany) && !TruckCompanies.Any(x => x.Name == sheetRow.TruckCompany))
                {
                    sheetTruckCompany = uow.TruckCompanyRepository.Add(new Entity { Name = sheetRow.TruckCompany });
                    TruckCompanies.Add(sheetTruckCompany);
                    isTruckCompanyNew = true;
                }

                var isCustomerNew = false;
                var sheetCustomer = Entities.FirstOrDefault(x => x.Name == sheetRow.Customer);
                if (sheetCustomer == null && !string.IsNullOrWhiteSpace(sheetRow.Customer) && !Entities.Any(x => x.Name == sheetRow.Customer))
                {
                    sheetCustomer = uow.EntityRepository.Add(new Entity { Name = sheetRow.Customer });
                    Entities.Add(sheetCustomer);
                    isCustomerNew = true;
                }

                transaction.TerminalID = newTerminal?.ID;

                transaction.DeliveryPointID = newDeliveryPoint?.ID;
                transaction.SpecificDeliveryPointID = newSpecificPoint?.ID;

                transaction.TransporterID = newTransporter?.ID;

                if (sheetCustomer != null)
                {
                    if (!isCustomerNew)
                    {
                        var u = CustomerToDeliveries.FirstOrDefault(x => x.DeliveryID == sheetRow.DeliveryID);
                        if (u != null)
                        {
                            u.Comment = sheetRow.CustomerNote;
                            ctdUpdate.Add(u);
                        }
                        else
                        {
                            ctdNew.Add(new CustomerToDelivery
                            {
                                DeliveryID = transaction.ID,
                                CustomerID = sheetCustomer.ID,
                                Comment = sheetRow.CustomerNote
                            });
                        }
                    }
                    else
                    {
                        ctdNew.Add(new CustomerToDelivery
                        {
                            DeliveryID = transaction.ID,
                            CustomerID = sheetCustomer.ID,
                            Comment = sheetRow.CustomerNote
                        });
                    }
                }

                if (sheetTruckCompany != null)
                {
                    if (!isTruckCompanyNew)
                    {
                        var u = TruckCompanyToDeliveries.FirstOrDefault(x => x.DeliveryID == sheetRow.DeliveryID);
                        if (u != null)
                        {
                            u.Comment = sheetRow.TruckLoadingCompanyComment;
                            ttdUpdate.Add(u);
                        }
                        else
                        {
                            ttdNew.Add(new TruckCompanyToDelivery
                            {
                                DeliveryID = transaction.ID,
                                TruckCompanyID = sheetTruckCompany.ID,
                                Comment = sheetRow.TruckLoadingCompanyComment
                            });
                        }
                    }
                    else
                    {
                        ttdNew.Add(new TruckCompanyToDelivery
                        {
                            DeliveryID = transaction.ID,
                            TruckCompanyID = sheetTruckCompany.ID,
                            Comment = sheetRow.TruckLoadingCompanyComment
                        });
                    }
                }

                transactionDtos.Add(transaction);
            }

            _logger.LogInformation("Inserting new {name} rows. Count: {count}", nameof(Transaction), transactionDtos.Count);
            uow.TransactionRepository.Add(transactionDtos, true);

            _logger.LogInformation("Inserting new {name} rows. Count: {count}", nameof(CustomerToDelivery), ctdNew.Count);
            uow.CustomerToDeliveryRepository.Add(ctdNew);

            _logger.LogInformation("Inserting new {name} rows. Count: {count}", nameof(TruckCompanyToDelivery), ttdNew.Count);
            uow.TruckCompanyToDeliveryRepository.Add(ttdNew);

            _logger.LogInformation("Updating {name} rows. Count: {count}", nameof(CustomerToDelivery), ctdUpdate.Count);
            uow.CustomerToDeliveryRepository.Update(ctdUpdate);

            _logger.LogInformation("Updating {name} rows. Count: {count}", nameof(TruckCompanyToDelivery), ttdUpdate.Count);
            uow.TruckCompanyToDeliveryRepository.Update(ttdUpdate);

            uow.Save("create_done");
        }
    }
}
