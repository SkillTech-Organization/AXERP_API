using AutoMapper;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.GoogleSheetModels;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.GoogleHelper.Managers;
using AXERP.API.GoogleHelper.Models;
using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Web.Http;

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

        #region SQL Scripts

        public readonly string Sql_Insert_GasTransaction = @"
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

        public readonly string Sql_Update_GasTransaction = @"
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

        public readonly string Sql_Select_GasTransaction = @"
                select * from GasTransactions where DeliveryID = '{0}'
            ";

        #endregion

        private ImportGasTransactionResponse ProcessRecords(ReadGoogleSheetResult<GasTransactionSheetModel> importResult)
        {
            var res = new ImportGasTransactionResponse
            {
                ImportedRows = importResult.RowCount,
                InvalidRows = importResult.InvalidRows,
                NewRows = 0,
                UpdatedRows = 0
            };

            if (importResult == null || importResult.Data == null)
            {
                throw new Exception("Failed google sheet import.");
            }

            // Invalid ids count
            res.InvalidRows += importResult.Data.Count(x => string.IsNullOrWhiteSpace(x.DeliveryID?.Trim()));

            // Only valid ids
            var filtered = importResult.Data.Where(x => !string.IsNullOrWhiteSpace(x.DeliveryID?.Trim()));

            var minSqlYear = 1753;

            using (var conn = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
            {
                foreach (var row in filtered)
                {
                    try
                    {
                        if (row.DateDelivered?.Year < minSqlYear || row.DateLoadedEnd?.Year < minSqlYear || row.CMR?.Year < minSqlYear || row.BillOfLading?.Year < minSqlYear)
                        {
                            importResult.InvalidRows++;
                            continue;
                        }
                        var oldRow = conn.QuerySingleOrDefault<GasTransaction>(string.Format(Sql_Select_GasTransaction, row.DeliveryID));
                        if (oldRow != null)
                        {
                            res.UpdatedRows++;
                            var affectedRows = conn.Execute(string.Format(Sql_Update_GasTransaction, row.DeliveryID), row);
                        }
                        else
                        {
                            res.NewRows++;
                            var affectedRows = conn.Execute(Sql_Insert_GasTransaction, row);
                        }
                    }
                    catch (Exception ex)
                    {
                        importResult.InvalidRows++;
                        _logger.LogError(ex, $"Cannot import record with id: {row.DeliveryID}");
                    }
                }
            }

            return res;
        }

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
                var importResult = await sheetService.ReadGoogleSheet<GasTransactionSheetModel>(sheet_id, $"{tab_name}{(range?.Length > 0 ? "!" : "")}{range}", sheetCulture);

                // Process
                var result = ProcessRecords(importResult);

                _logger.LogInformation("GasTransactions imported. Stats: {stats}", Newtonsoft.Json.JsonConvert.SerializeObject(result));

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while importing GasTransactions");
                var res = new ObjectResult(new ImportGasTransactionResponse
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError,
                    Error = ex.Message
                })
                {
                    StatusCode = 500
                };
                return res;
            }
        }
    }
}
