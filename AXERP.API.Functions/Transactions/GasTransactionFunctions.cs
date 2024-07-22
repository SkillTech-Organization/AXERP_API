using AXERP.API.Domain.Entities;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.Functions.SheetProcessors;
using AXERP.API.GoogleHelper.Managers;
using AXERP.API.GoogleHelper.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using YamlDotNet.Core;
using AXERP.API.Persistence.Repositories;
using AXERP.API.Persistence.ServiceContracts.Requests;
using AXERP.API.Persistence.Utils;
using AutoMapper;
using Location = AXERP.API.Domain.Entities.Location;

namespace AXERP.API.Functions.Transactions
{
    public class GasTransactionFunctions
    {
        private readonly ILogger<GasTransactionFunctions> _logger;
        private readonly GasTransactionSheetProcessor _gasTransactionSheetProcessor;
        private readonly GenericRepository _genericRepository;
        private readonly IMapper _mapper;

        public GasTransactionFunctions(
            ILogger<GasTransactionFunctions> logger,
            GasTransactionSheetProcessor gasTransactionSheetProcessor,
            GenericRepository genericRepository,
            IMapper mapper)
        {
            _logger = logger;
            _gasTransactionSheetProcessor = gasTransactionSheetProcessor;
            _genericRepository = genericRepository;
            _mapper = mapper;
        }

        #region SQL Scripts

        public readonly string Sql_Insert_Delivery = @"
                INSERT INTO Deliveries
                       (ID
                       ,DateLoadedEnd
                       ,DateDelivered
                       ,SalesContractID
                       ,SalesStatus
                       ,TerminalID
                       ,QtyLoaded
                       ,ToDeliveryID
                       ,Status
                       ,SpecificDeliveryPointID
                       ,DeliveryPointID
                       ,TransporterID
                       ,DeliveryUP
                       ,TransportCharges
                       ,UnitSlotCharge
                       ,ServiceCharges
                       ,UnitStorageCharge
                       ,StorageCharge
                       ,OtherCharges
                       ,Sales
                       ,CMR
                       ,BioMWh
                       ,BillOfLading
                       ,BioAddendum
                       ,Comment
                       ,ReferenceID1
                       ,ReferenceID2
                       ,ReferenceID3)
                 VALUES
                       (@ID
                       ,@DateLoadedEnd
                       ,@DateDelivered
                       ,@SalesContractID
                       ,@SalesStatus
                       ,@TerminalID
                       ,@QtyLoaded
                       ,@ToDeliveryID
                       ,@Status
                       ,@SpecificDeliveryPointID
                       ,@DeliveryPointID
                       ,@TransporterID
                       ,@DeliveryUP
                       ,@TransportCharges
                       ,@UnitSlotCharge
                       ,@ServiceCharges
                       ,@UnitStorageCharge
                       ,@StorageCharge
                       ,@OtherCharges
                       ,@Sales
                       ,@CMR
                       ,@BioMWh
                       ,@BillOfLading
                       ,@BioAddendum
                       ,@Comment
                       ,@ReferenceID1
                       ,@ReferenceID2
                       ,@ReferenceID3)
            ";

        public readonly string Sql_Select_Delivery_IDs = @"
                select ID from Deliveries
            ";

        public readonly string Sql_Query_Paged_GasTransactions = 
            @"
            select X.* from 
                (select _table.*, ROW_NUMBER() OVER (/**orderby**/) AS RowNumber from GasTransactions _table /**where**/)
            as X where RowNumber between @start and @finish
            ";

        public readonly string Sql_Query_Paged_GasTransactions_Dynamic_Columns =
            @"
            select /**select**/ from 
                (select _table.*, ROW_NUMBER() OVER (/**orderby**/) AS RowNumber from GasTransactions _table /**where**/)
            as X where RowNumber between @start and @finish
            ";

        public readonly string Sql_Query_Count_GasTransactions = "SELECT COUNT(*) FROM GasTransactions";

        #endregion

        private ImportGasTransactionResponse InsertTransactions(GenericSheetImportResult<GasTransaction> importResult)
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

            using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        /*
                         * FILTER NEW DATA
                         */

                        // New Deliveries row check
                        var ids = conn.Query<string>(Sql_Select_Delivery_IDs, transaction: transaction);
                        var newSheetRows = importResult.Data.Where(x => !ids.Contains(x.DeliveryID));

                        var locations = conn.Query<Location>("select * from Locations", transaction: transaction).ToList();
                        var entities = conn.Query<Entity>("select * from Entities", transaction: transaction).ToList();
                        var truckCompanies = conn.Query<TruckCompany>("select * from TruckCompanies", transaction: transaction).ToList();
                        var customers = conn.Query<Customer>("select * from Customers", transaction: transaction).ToList();
                        var transporters = conn.Query<Transporter>("select * from Transporters", transaction: transaction).ToList();

                        var ctd = new List<CustomerToDelivery>();
                        var ttd = new List<TruckCompanyToDelivery>();

                        var deliveries = new List<Delivery>();

                        foreach (var newSheetRow in newSheetRows)
                        {
                            var delivery = _mapper.Map<Delivery>(newSheetRow);

                            delivery.ID = newSheetRow.DeliveryID;

                            var newTerminal = locations.FirstOrDefault(x => x.Name == newSheetRow.Terminal);
                            if (newTerminal == null && !string.IsNullOrWhiteSpace(newSheetRow.Terminal) && !locations.Any(x => x.Name == newSheetRow.Terminal))
                            {
                                newTerminal = conn.QuerySingle<Location>(@"INSERT INTO Locations (Name) OUTPUT INSERTED.ID, INSERTED.NAME VALUES (@Name)", new { Name = newSheetRow.Terminal }, transaction: transaction);
                                locations.Add(newTerminal);
                            }

                            var newDeliveryPoint = locations.FirstOrDefault(x => x.Name == newSheetRow.DeliveryPoint);
                            if (newDeliveryPoint == null && !string.IsNullOrWhiteSpace(newSheetRow.DeliveryPoint) && !locations.Any(x => x.Name == newSheetRow.DeliveryPoint))
                            {
                                newDeliveryPoint = conn.QuerySingle<Location>(@"INSERT INTO Locations (Name) OUTPUT INSERTED.ID, INSERTED.NAME VALUES (@Name)", new { Name = newSheetRow.DeliveryPoint }, transaction: transaction);
                                locations.Add(newDeliveryPoint);
                            }

                            var newSpecificPoint = locations.FirstOrDefault(x => x.Name == newSheetRow.SpecificDeliveryPoint);
                            if (newSpecificPoint == null && !string.IsNullOrWhiteSpace(newSheetRow.SpecificDeliveryPoint) && !locations.Any(x => x.Name == newSheetRow.SpecificDeliveryPoint))
                            {
                                newSpecificPoint = conn.QuerySingle<Location>(@"INSERT INTO Locations (Name) OUTPUT INSERTED.ID, INSERTED.NAME VALUES (@Name)", new { Name = newSheetRow.SpecificDeliveryPoint }, transaction: transaction);
                                locations.Add(newSpecificPoint);
                            }

                            var newRef = entities.FirstOrDefault(x => x.Name == newSheetRow.Reference);
                            if (newRef == null && !string.IsNullOrWhiteSpace(newSheetRow.Reference) && !entities.Any(x => x.Name == newSheetRow.Reference))
                            {
                                newRef = conn.QuerySingle<Entity>(@"INSERT INTO Entities (Name) OUTPUT INSERTED.ID, INSERTED.NAME VALUES (@Name)", new { Name = newSheetRow.Reference }, transaction: transaction);
                                entities.Add(newRef);
                            }

                            var newRef2 = entities.FirstOrDefault(x => x.Name == newSheetRow.Reference2);
                            if (newRef2 == null && !string.IsNullOrWhiteSpace(newSheetRow.Reference2) && !entities.Any(x => x.Name == newSheetRow.Reference2))
                            {
                                newRef2 = conn.QuerySingle<Entity>(@"INSERT INTO Entities (Name) OUTPUT INSERTED.ID, INSERTED.NAME VALUES (@Name)", new { Name = newSheetRow.Reference2 }, transaction: transaction);
                                entities.Add(newRef2);
                            }

                            var newRef3 = entities.FirstOrDefault(x => x.Name == newSheetRow.Reference3);
                            if (newRef3 == null && !string.IsNullOrWhiteSpace(newSheetRow.Reference3) && !entities.Any(x => x.Name == newSheetRow.Reference3))
                            {
                                newRef3 = conn.QuerySingle<Entity>(@"INSERT INTO Entities (Name) OUTPUT INSERTED.ID, INSERTED.NAME VALUES (@Name)", new { Name = newSheetRow.Reference3 }, transaction: transaction);
                                entities.Add(newRef3);
                            }

                            var newTransporter = transporters.FirstOrDefault(x => x.Name == newSheetRow.Transporter);
                            if (newTransporter == null && !string.IsNullOrWhiteSpace(newSheetRow.Transporter) && !transporters.Any(x => x.Name == newSheetRow.Transporter))
                            {
                                newTransporter = conn.QuerySingle<Transporter>(@"INSERT INTO Transporters (Name) OUTPUT INSERTED.ID, INSERTED.NAME VALUES (@Name)", new { Name = newSheetRow.Transporter }, transaction: transaction);
                                transporters.Add(newTransporter);
                            }

                            var newTruckCompany = truckCompanies.FirstOrDefault(x => x.Name == newSheetRow.TruckCompany);
                            if (newTruckCompany == null && !string.IsNullOrWhiteSpace(newSheetRow.TruckCompany) && !truckCompanies.Any(x => x.Name == newSheetRow.TruckCompany))
                            {
                                newTruckCompany = conn.QuerySingle<TruckCompany>(@"INSERT INTO TruckCompanies (Name) OUTPUT INSERTED.ID, INSERTED.NAME VALUES (@Name)", new { Name = newSheetRow.TruckCompany }, transaction: transaction);
                                truckCompanies.Add(newTruckCompany);
                            }

                            var newCustomer = customers.FirstOrDefault(x => x.Name == newSheetRow.Customer);
                            if (newCustomer == null && !string.IsNullOrWhiteSpace(newSheetRow.Customer) && !customers.Any(x => x.Name == newSheetRow.Customer))
                            {
                                newCustomer = conn.QuerySingle<Customer>(@"INSERT INTO Customers (Name) OUTPUT INSERTED.ID, INSERTED.NAME VALUES (@Name)", new { Name = newSheetRow.Customer }, transaction: transaction);
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

                            //delivery.TruckCompanyToDeliveryID = newTruckCompany?.ID;
                            //delivery.CustomerToDeliveryID = newCustomer?.ID;

                            deliveries.Add(delivery);
                        }

                        transaction.Save("pt-before-deliveries");

                        // New Deliveries
                        res.NewRowsInsertedIntoDatabase += conn.Execute(Sql_Insert_Delivery, deliveries, transaction: transaction);

                        transaction.Save("pt-before-ctd-ttd");

                        conn.Execute(@"INSERT INTO CustomerToDelivery (DeliveryID, CustomerID, Comment) VALUES (@DeliveryID, @CustomerID, @Comment)", ctd, transaction: transaction);
                        conn.Execute(@"INSERT INTO TruckCompanyToDelivery (DeliveryID, TruckCompanyID, Comment) VALUES (@DeliveryID, @TruckCompanyID, @Comment)", ttd, transaction: transaction);

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

            return res;
        }

        //[Function(nameof(ClearDatabaseDeliveries))]
        //[OpenApiOperation(operationId: nameof(ClearDatabaseDeliveries), tags: new[] { "database" })]
        //[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        //public async Task<IActionResult> ClearDatabaseDeliveries([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        //{
        //    _logger.LogInformation("Clearing Deliveries and associated data from database...");


        //}

        [Function(nameof(ImportGasTransactions))]
        [OpenApiOperation(operationId: nameof(ImportGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> ImportGasTransactions([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Importing GasTransactions...");

            try
            {
                // Get parameters
                var credentialsJson = Environment.GetEnvironmentVariable("GoogleCredentials");

                if (string.IsNullOrWhiteSpace(credentialsJson))
                {
                    var msg = "GoogleCredentials environment variable is missing.";
                    _logger.LogError(msg);
                    return new BadRequestObjectResult(msg);
                }

                var sheet_id = Environment.GetEnvironmentVariable("BulkDeliveriesSheetDataSheetId");
                var tab_name = Environment.GetEnvironmentVariable("BulkDeliveriesSheetDataGasTransactionsTab");
                var range = Environment.GetEnvironmentVariable("BulkDeliveriesSheetDataGasTransactionRange");
                var sheetCulture = Environment.GetEnvironmentVariable("SheetCulture") ?? "fr-FR";

                // Sheet import
                var sheetService = new GoogleSheetManager(credentials: credentialsJson, format: CredentialsFormats.Text);

                var rows = await sheetService.ReadGoogleSheetRaw(sheet_id, $"{tab_name}{(range?.Length > 0 ? "!" : "")}{range}");
                var importResult = _gasTransactionSheetProcessor.ProcessRows(rows, sheetCulture);

                // Process
                var result = InsertTransactions(importResult);

                _logger.LogInformation("GasTransactions imported. Stats: {stats}", Newtonsoft.Json.JsonConvert.SerializeObject(result));

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while importing GasTransactions");
                var res = new ObjectResult(new ImportGasTransactionResponse
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError,
                    RequestError = ex.Message
                })
                {
                    StatusCode = 500
                };
                return res;
            }
        }

        /*
        [Function(nameof(GetAllGasTransactions))]
        [OpenApiOperation(operationId: nameof(GetAllGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public IActionResult GetAllGasTransactions(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
                [SqlInput("%Sql_Query_All_GasTransactions%", "SqlConnectionString")] IEnumerable<GasTransaction> items)
        {
            _logger.LogInformation("Querying GasTransactions. Row count: {count}", items.Count());

            return new OkObjectResult(items);
        }

        [Function(nameof(QueryPagedGasTransactions))]
        [OpenApiOperation(operationId: nameof(QueryPagedGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiParameter(name: "PageSize", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "Returned row count")]
        //[OpenApiParameter(name: "OrderBy", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "Order by")]
        [OpenApiParameter(name: "Page", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "Page")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public IActionResult QueryPagedGasTransactions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            //[SqlInput("%Sql_Query_Count_GasTransactions%", "SqlConnectionString")] Dictionary<string, int> totalCount, // cannot be int or string!
            [SqlInput(
                commandText: "%Sql_Query_Paged_GasTransactions%",
                parameters: "@PageSize={Query.PageSize},@Page={Query.Page}", // @OrderBy={Query.OrderBy},
                connectionStringSetting: "SqlConnectionString")] IEnumerable<GasTransaction> items)
        {
            _logger.LogInformation("Querying GasTransactions. Row count: {count}, Page: {page}, PageSize: {pageSize}", items.Count(), req.Query["Page"], req.Query["PageSize"]);

            var totalCount = CountTableRows("GasTransactions");

            _logger.LogInformation("Querying GasTransactions. TotalCount: {totalCount}", totalCount);

            return new OkObjectResult(new GenericQueryResponse<GasTransaction>
            {
                Data = items,
                PageIndex = int.Parse(req.Query["Page"]),
                PageSize = int.Parse(req.Query["PageSize"]),
                TotalCount = totalCount
            });
        }
        */

        [Function(nameof(CountGasTransactions))]
        [OpenApiOperation(operationId: nameof(CountGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public int CountGasTransactions(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            return _genericRepository.Count(Environment.GetEnvironmentVariable(nameof(Sql_Query_Count_GasTransactions)) ?? Sql_Query_Count_GasTransactions);
        }

        [Function(nameof(QueryGasTransactions))]
        [OpenApiOperation(operationId: nameof(QueryGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiParameter(name: "Search", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Search in all columns, type Column = Search for specific search, eg. DeliveryID = 5")]
        [OpenApiParameter(name: "SearchOnlyInSelectedColumns", In = ParameterLocation.Query, Required = false, Type = typeof(bool), Description = "Search only in columns provided in the Columns parameter - ignored if Search is written for specific column")]
        [OpenApiParameter(name: "Columns", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "List of columns, separated by ',' character, all columns will be used by default")]
        [OpenApiParameter(name: "OrderBy", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Order by column, default is DeliveryID")]
        [OpenApiParameter(name: "OrderByDesc", In = ParameterLocation.Query, Required = false, Type = typeof(bool), Description = "Descending order, false by default")]
        [OpenApiParameter(name: "PageSize", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Returned row count, default is 5")]
        [OpenApiParameter(name: "Page", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page index, starting from 1 (0 will be interpreted as 1), default is 1")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public IActionResult QueryGasTransactions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var queryTemplate = Environment.GetEnvironmentVariable(nameof(Sql_Query_Paged_GasTransactions_Dynamic_Columns)) ?? Sql_Query_Paged_GasTransactions_Dynamic_Columns;
            var countTemplate = Environment.GetEnvironmentVariable(nameof(Sql_Query_Count_GasTransactions)) ?? Sql_Query_Count_GasTransactions;

            var cols = req.Query["Columns"]?.ToString()?.Split(",", StringSplitOptions.TrimEntries)?.ToList() ?? new List<string>();

            var page = int.Parse(req.Query["Page"] ?? "1");
            if (page <= 0)
            {
                page = 1;
            }

            var pageSize = int.Parse(req.Query["PageSize"] ?? "5");
            if (pageSize <= 0)
            {
                pageSize = 1;
            }

            var result = _genericRepository.PagedQuery<GasTransaction>(new PagedQueryRequest
            {
                QueryTemplate = queryTemplate,
                CountTemplate = countTemplate,
                Columns = cols,
                OrderBy = req.Query["OrderBy"] ?? "DeliveryID",
                OrderDesc = bool.Parse(req.Query["OrderByDesc"] ?? "false"),
                Page = page,
                PageSize = pageSize,
                Search = req.Query["Search"],
                SearchOnlyInSelectedColumns = bool.Parse(req.Query["SearchOnlyInSelectedColumns"] ?? "false")
            });

            return new OkObjectResult(result);
        }

    }
}
