using AXERP.API.Domain;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.GoogleHelper.Managers;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;
using Newtonsoft.Json;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AXERP.API.Business.Commands
{
    [ForSystem("SQL Server", LogConstants.FUNCTION_GOOGLE_SYNC)]
    public partial class UpdateBillOfLadingCommand : BaseAuditedClass<UpdateBillOfLadingCommand>
    {
        private readonly UnitOfWorkFactory _uowFactory;

        [GeneratedRegex("(?<id>[0-9]+)(?<suffix>[^0-9]{0,})", RegexOptions.IgnoreCase, "hu-HU")]
        private static partial Regex DeliveryIdRegex();

        public UpdateBillOfLadingCommand(
            AxerpLoggerFactory axerpLoggerFactory,
            UnitOfWorkFactory uowFactory) : base(axerpLoggerFactory)
        {
            _uowFactory = uowFactory;
        }

        private void UpdateSheetBillOfLadings(GoogleSheetManager sheetService, List<string> deliveryIds, DateTime billOfLading, IList<IList<object>> rows)
        {
            // Env
            var sheet_id = EnvironmentHelper.TryGetParameter("BulkDeliveriesSheetDataSheetId");
            var tab_name = EnvironmentHelper.TryGetParameter("BulkDeliveriesSheetDataGasTransactionsTab");
            var sheetCulture = EnvironmentHelper.TryGetParameter("SheetCulture") ?? "fr-FR";
            var sheetBillOfLadingColumn = EnvironmentHelper.TryGetOptionalParameter("SheetBillOfLadingColumn") ?? "BV";

            // Preprocess
            var headers = rows[0];
            var sheet_rows = rows.Skip(1).ToList();

            // Eg. DeliveryID -> 0 (indexof Delivery ID in sheet headers)
            var field_names = new Dictionary<string, int>();
            foreach (var property in typeof(Delivery).GetProperties())
            {
                var jsonAttribute = property.GetCustomAttribute<JsonPropertyAttribute>(true);
                if (jsonAttribute != null)
                {
                    var propertyName = jsonAttribute.PropertyName;
                    field_names[property.Name] = headers.IndexOf(propertyName ?? property.Name);
                }
            }

            var billOfLadingFormatted = billOfLading.ToString("G", new CultureInfo(sheetCulture));

            // Breaks reference
            var ids = deliveryIds.ToList();

            // Updating cells
            for (int rowIndex = 0; rowIndex < sheet_rows.Count; rowIndex++)
            {
                var row = sheet_rows[rowIndex];
                var deliveryIdIdx = field_names[nameof(Delivery.DeliveryID)];

                // No DeliveryID in this row
                if (row.Count <= deliveryIdIdx || row[deliveryIdIdx] == null || string.IsNullOrWhiteSpace(row[deliveryIdIdx].ToString()))
                {
                    continue;
                }

                var rawDeliveryId = row[deliveryIdIdx].ToString()!.Trim();

                if (string.IsNullOrWhiteSpace(rawDeliveryId) && ids.Contains(rawDeliveryId))
                {
                    var result = sheetService.UpdateCell(sheet_id, tab_name, sheetBillOfLadingColumn, rowIndex + 1, billOfLadingFormatted);
                    ids.Remove(rawDeliveryId);
                }

                if (!ids.Any())
                {
                    break;
                }
            }
        }

        private async Task<BaseResponse> WriteBackSheet(List<string> deliveryIds, DateTime billOfLading)
        {
            var res = new BaseResponse();

            try
            {
                var credentialsJson = EnvironmentHelper.TryGetParameter("GoogleCredentials");

                var sheet_id = EnvironmentHelper.TryGetParameter("BulkDeliveriesSheetDataSheetId");
                var tab_name = EnvironmentHelper.TryGetParameter("BulkDeliveriesSheetDataGasTransactionsTab");
                var range = EnvironmentHelper.TryGetOptionalParameter("BulkDeliveriesSheetDataGasTransactionRange");

                var sheetService = new GoogleSheetManager(credentials: credentialsJson, format: CredentialsFormats.Text);

                var rows = await sheetService.ReadGoogleSheetRaw(sheet_id, $"{tab_name}{(range?.Length > 0 ? "!" : "")}{range}");

                UpdateSheetBillOfLadings(sheetService, deliveryIds, billOfLading, rows);
            }
            catch (Exception ex)
            {
                throw;
            }

            return res;
        }

        private async Task<BaseResponse> WriteBackDb(List<string> deliveryIds, DateTime billOfLading)
        {
            var res = new BaseResponse();

            using (var uow = _uowFactory.Create())
            {
                try
                {
                    foreach (var deliveryID in deliveryIds)
                    {
                        var matches = DeliveryIdRegex().Matches(deliveryID);

                        var id = int.Parse(matches[0].Groups["id"].Value.Trim());
                        var sf = matches[0].Groups["suffix"].Value.Trim();

                        var delivery = uow.TransactionRepository.GetById(id, sf);

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
                }
                catch (Exception ex)
                {
                    uow.Rollback();
                    throw;
                }
            }

            return res;
        }

        public async Task<BaseResponse> Execute(List<string> deliveryIds)
        {
            var res = new BaseResponse();

            var billOfLading = DateTime.Now;

            var sheetResult = await WriteBackSheet(deliveryIds, billOfLading);
            if (!sheetResult.IsSuccess)
            {
                return sheetResult;
            }

            var dbResult = await WriteBackDb(deliveryIds, billOfLading);
            if (!dbResult.IsSuccess)
            {
                return dbResult;
            }

            return res;
        }
    }
}
