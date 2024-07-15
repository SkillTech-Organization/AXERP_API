using AutoMapper;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.GoogleSheetModels;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.GoogleHelper.Managers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Sql;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Runtime.InteropServices;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AXERP.API.Functions.Transactions
{
    public class GasTransactionFunctions
    {
        private readonly ILogger<GasTransactionFunctions> _logger;

        private readonly IMapper _mapper;

        public GasTransactionFunctions(ILogger<GasTransactionFunctions> logger, IMapper mapper)
        {
            _logger = logger;
            _mapper = mapper;
        }

        [Function(nameof(ImportGasTransactions))]
        [OpenApiOperation(operationId: nameof(ImportGasTransactions), tags: new[] { "gas-transactions" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> ImportGasTransactions([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Importing GasTransactions...");

            var sheet_id = Environment.GetEnvironmentVariable("BulkDeliveriesSheetData:SheetId");
            var tab_name = Environment.GetEnvironmentVariable("BulkDeliveriesSheetData:GasTransactionsTab");
            var range = Environment.GetEnvironmentVariable("BulkDeliveriesSheetData:GasTransactionRange");
            var sheetCulture = Environment.GetEnvironmentVariable("SheetCulture") ?? "fr-FR";

            var sheetService = new GoogleSheetManager();
            var importResult = await sheetService.ReadGoogleSheet<GasTransactionSheetModel>(sheet_id, $"{tab_name}{(range?.Length > 0 ? "!" : "")}{range}", sheetCulture);

            var postImportMiscInvalidCount = importResult.Data.Count(x => string.IsNullOrWhiteSpace(x.DeliveryID?.Trim()));
            importResult.InvalidRows += postImportMiscInvalidCount;

            var result = _mapper.Map<List<GasTransaction>>(importResult.Data.Where(x => !string.IsNullOrWhiteSpace(x.DeliveryID?.Trim())));

            var stats = $"GasTransactions imported. Row count: {importResult.RowCount}. Failed to import: {importResult.InvalidRows}";
            _logger.LogInformation(stats);

            var insert_sql = @"
                INSERT INTO GasTransactions
                       (DeliveryID
                       ,DateLoadedEnd
                       ,DateDelivered
                       ,SalesContractID
                       ,SalesStatus
                       ,Terminal
                       ,QtyLoaded
                       ,ToDeliveryID
                       ,Status
                       ,SpecificDeliveryPoint
                       ,DeliveryPoint
                       ,Transporter
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
                       ,CustomerNote
                       ,Customer
                       ,Reference
                       ,Reference2
                       ,Reference3
                       ,TruckLoadingCompanyComment
                       ,TruckCompany)
                 VALUES
                       (@DeliveryID
                       ,@DateLoadedEnd
                       ,@DateDelivered
                       ,@SalesContractID
                       ,@SalesStatus
                       ,@Terminal
                       ,@QtyLoaded
                       ,@ToDeliveryID
                       ,@Status
                       ,@SpecificDeliveryPoint
                       ,@DeliveryPoint
                       ,@Transporter
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
                       ,@CustomerNote
                       ,@Customer
                       ,@Reference
                       ,@Reference2
                       ,@Reference3
                       ,@TruckLoadingCompanyComment
                       ,@TruckCompany)
            ";

            var update_sql = @"
                UPDATE GasTransactions
                   SET DeliveryID = @DeliveryID
                      ,DateLoadedEnd = @DateLoadedEnd
                      ,DateDelivered = @DateDelivered
                      ,SalesContractID = @SalesContractID
                      ,SalesStatus = @SalesStatus
                      ,Terminal = @Terminal
                      ,QtyLoaded = @QtyLoaded
                      ,ToDeliveryID = @ToDeliveryID
                      ,Status = @Status
                      ,SpecificDeliveryPoint = @SpecificDeliveryPoint
                      ,DeliveryPoint = @DeliveryPoint
                      ,Transporter = @Transporter
                      ,DeliveryUP = @DeliveryUP
                      ,TransportCharges = @TransportCharges
                      ,UnitSlotCharge = @UnitSlotCharge
                      ,ServiceCharges = @ServiceCharges
                      ,UnitStorageCharge = @UnitStorageCharge
                      ,StorageCharge = @StorageCharge
                      ,OtherCharges = @OtherCharges
                      ,Sales = @Sales
                      ,CMR = @CMR
                      ,BioMWh = @BioMWh
                      ,BillOfLading = @BillOfLading
                      ,BioAddendum = @BioAddendum
                      ,Comment = @Comment
                      ,CustomerNote = @CustomerNote
                      ,Customer = @Customer
                      ,Reference = @Reference
                      ,Reference2 = @Reference2
                      ,Reference3 = @Reference3
                      ,TruckLoadingCompanyComment = @TruckLoadingCompanyComment
                      ,TruckCompany = @TruckCompany
                where DeliveryID = '{0}'
            ";

            var select_sql = @"
                select * from GasTransactions where DeliveryID = '{0}'
            ";

            var newRecords = 0;
            var updatedRecords = 0;

            try
            {

                using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
                {
                    foreach (var row in result)
                    {
                        if (row.DateDelivered?.Year < 1753 || row.DateLoadedEnd?.Year < 1753 || row.CMR?.Year < 1753 || row.BillOfLading?.Year < 1753) // row.DeliveryID == "1028b" || 
                        {
                            importResult.InvalidRows++;
                            continue;
                        }
                        var oldRow = conn.QuerySingleOrDefault<GasTransaction>(string.Format(select_sql, row.DeliveryID));
                        if (oldRow != null)
                        {
                            updatedRecords++;
                            var affectedRows = conn.Execute(string.Format(update_sql, row.DeliveryID), row);
                        }
                        else
                        {
                            newRecords++;
                            var affectedRows = conn.Execute(insert_sql, row);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return new OkObjectResult(new ImportGasTransactionResponse
            {
                ImportedRows = importResult.RowCount,
                InvalidRows = importResult.InvalidRows,
                NewRows = newRecords,
                UpdatedRows = updatedRecords
            });

            //return new GasTransactionImportResponse
            //{
            //    Transactions = result,
            //    HttpResponse = new OkObjectResult(new ImportGasTransactionResponse
            //    {
            //        ImportedRows = importResult.RowCount,
            //        InvalidRows = importResult.InvalidRows,
            //    })
            //};
        }
    }
}
