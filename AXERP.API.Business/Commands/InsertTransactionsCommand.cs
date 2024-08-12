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

                    CacheBusinessData(uow);

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

                    /*
                     * UPDATE TRANSACTIONS
                     */
                    _logger.LogInformation("Updating transactions...");

                    Update(uow, updatedSheetRows);

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

        private void CacheBusinessData(IUnitOfWork uow)
        {
            Transactions = uow.TransactionRepository.GetAll().ToList();
            TransactionIds = Transactions.Select(x => x.ID).ToList();
            Interfaces = uow.InterfaceRepository.GetAll().ToList();
            Statuses = uow.TransactionStatusRepository.GetAll().Select(x => x.Name).ToList();
            Documents = uow.DocumentRepository.GetAll().ToList();
            TruckCompanies = uow.TruckCompanyRepository.GetAll().ToList();
            Entities = uow.EntityRepository.GetAll().ToList();
        }

        private void Delete(IUnitOfWork uow, IEnumerable<string> ids)
        {
            uow.CustomerToDeliveryRepository.Delete(nameof(CustomerToDelivery.DeliveryID), ids);
            uow.TruckCompanyToDeliveryRepository.Delete(nameof(CustomerToDelivery.DeliveryID), ids);

            uow.TransactionRepository.Delete(ids);

            uow.Save("delete_done");
        }

        private void Update(IUnitOfWork uow, IEnumerable<Delivery> sheetRows)
        {
            if (!sheetRows.Any())
            {
                return;
            }

            var transactionDtos = new List<Transaction>();
            var ctd = new List<CustomerToDelivery>();
            var ttd = new List<TruckCompanyToDelivery>();

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

                var newTruckCompany = TruckCompanies.FirstOrDefault(x => x.Name == sheetRow.TruckCompany);
                if (newTruckCompany == null && !string.IsNullOrWhiteSpace(sheetRow.TruckCompany) && !TruckCompanies.Any(x => x.Name == sheetRow.TruckCompany))
                {
                    newTruckCompany = uow.TruckCompanyRepository.Add(new Entity { Name = sheetRow.TruckCompany });
                    TruckCompanies.Add(newTruckCompany);
                }

                var newCustomer = Entities.FirstOrDefault(x => x.Name == sheetRow.Customer);
                if (newCustomer == null && !string.IsNullOrWhiteSpace(sheetRow.Customer) && !Entities.Any(x => x.Name == sheetRow.Customer))
                {
                    newCustomer = uow.EntityRepository.Add(new Entity { Name = sheetRow.Customer });
                    Entities.Add(newCustomer);
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

                transactionDtos.Add(transaction);
            }

            uow.TransactionRepository.Update(transactionDtos);

            uow.CustomerToDeliveryRepository.Add(ctd);
            uow.TruckCompanyToDeliveryRepository.Add(ttd);

            uow.Save("update_done");
        }

        private void Create(IUnitOfWork uow, IEnumerable<Delivery> sheetRows)
        {
            if (!sheetRows.Any())
            {
                return;
            }

            var transactionDtos = new List<Transaction>();
            var ctd = new List<CustomerToDelivery>();
            var ttd = new List<TruckCompanyToDelivery>();

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

                var newTruckCompany = TruckCompanies.FirstOrDefault(x => x.Name == sheetRow.TruckCompany);
                if (newTruckCompany == null && !string.IsNullOrWhiteSpace(sheetRow.TruckCompany) && !TruckCompanies.Any(x => x.Name == sheetRow.TruckCompany))
                {
                    newTruckCompany = uow.TruckCompanyRepository.Add(new Entity { Name = sheetRow.TruckCompany });
                    TruckCompanies.Add(newTruckCompany);
                }

                var newCustomer = Entities.FirstOrDefault(x => x.Name == sheetRow.Customer);
                if (newCustomer == null && !string.IsNullOrWhiteSpace(sheetRow.Customer) && !Entities.Any(x => x.Name == sheetRow.Customer))
                {
                    newCustomer = uow.EntityRepository.Add(new Entity { Name = sheetRow.Customer });
                    Entities.Add(newCustomer);
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

                transactionDtos.Add(transaction);
            }

            uow.TransactionRepository.Add(transactionDtos, true);

            uow.CustomerToDeliveryRepository.Add(ctd);
            uow.TruckCompanyToDeliveryRepository.Add(ttd);

            uow.Save("create_done");
        }
    }
}
