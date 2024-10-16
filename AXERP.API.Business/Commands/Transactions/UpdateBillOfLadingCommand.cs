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

        private void UpdateSheetBillOfLadings(GoogleSheetManager sheetService, List<string> fileNames, DateTime billOfLading, IList<IList<object>> rows)
        {
            // Env
            var sheet_id = EnvironmentHelper.TryGetParameter("BulkDeliveriesSheetDataSheetId");
            var regexPattern = EnvironmentHelper.TryGetParameter("BlobStorePdfFileRegexPattern");
            var regexReferenceKey = EnvironmentHelper.TryGetParameter("RegexReferenceKey");
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

            // BL File references in the form they can be found in the Google Sheet
            var blFileReferences = new List<string>();
            foreach (var fileName in fileNames)
            {
                var rawFileName = Path.GetFileName(fileName);
                var matches = Regex.Matches(rawFileName, regexPattern, RegexOptions.IgnoreCase);
                if (matches.Count == 0)
                {
                    continue;
                }
                var referenceName = matches[0].Groups[regexReferenceKey].Value.Trim();
                blFileReferences.Add(referenceName);
            }

            // Updating cells
            for (int rowIndex = 0; rowIndex < sheet_rows.Count; rowIndex++)
            {
                var row = sheet_rows[rowIndex];

                var ref1Idx = field_names[nameof(Delivery.Reference)];
                var ref2Idx = field_names[nameof(Delivery.Reference2)];
                var ref3Idx = field_names[nameof(Delivery.Reference3)];

                if (!(row.Count <= ref1Idx || row[ref1Idx] == null || string.IsNullOrWhiteSpace(row[ref1Idx].ToString())))
                {
                    var rawRef1 = row[ref1Idx].ToString()!.Trim();
                    if (!string.IsNullOrWhiteSpace(rawRef1) && blFileReferences.Contains(rawRef1))
                    {
                        var result = sheetService.UpdateCell(sheet_id, tab_name, sheetBillOfLadingColumn, rowIndex + 1, billOfLadingFormatted);
                        blFileReferences.Remove(rawRef1);
                        if (!blFileReferences.Any())
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                if (!(row.Count <= ref2Idx || row[ref2Idx] == null || string.IsNullOrWhiteSpace(row[ref2Idx].ToString())))
                {
                    var rawRef1 = row[ref2Idx].ToString()!.Trim();
                    if (!string.IsNullOrWhiteSpace(rawRef1) && blFileReferences.Contains(rawRef1))
                    {
                        var result = sheetService.UpdateCell(sheet_id, tab_name, sheetBillOfLadingColumn, rowIndex + 1, billOfLadingFormatted);
                        blFileReferences.Remove(rawRef1);
                        if (!blFileReferences.Any())
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                if (!(row.Count <= ref3Idx || row[ref3Idx] == null || string.IsNullOrWhiteSpace(row[ref3Idx].ToString())))
                {
                    var rawRef1 = row[ref3Idx].ToString()!.Trim();
                    if (!string.IsNullOrWhiteSpace(rawRef1) && blFileReferences.Contains(rawRef1))
                    {
                        var result = sheetService.UpdateCell(sheet_id, tab_name, sheetBillOfLadingColumn, rowIndex + 1, billOfLadingFormatted);
                        blFileReferences.Remove(rawRef1);
                        if (!blFileReferences.Any())
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
        }

        private async Task<BaseResponse> WriteBackSheet(List<string> fileNames, DateTime billOfLading)
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

                UpdateSheetBillOfLadings(sheetService, fileNames, billOfLading, rows);
            }
            catch (Exception ex)
            {
                throw;
            }

            return res;
        }

        //private async Task<BaseResponse> WriteBackDb(List<string> deliveryIds, DateTime billOfLading)
        //{
        //    var res = new BaseResponse();

        //    using (var uow = _uowFactory.Create())
        //    {
        //        try
        //        {
        //            foreach (var deliveryID in deliveryIds)
        //            {
        //                var matches = DeliveryIdRegex().Matches(deliveryID);

        //                var id = int.Parse(matches[0].Groups["id"].Value.Trim());
        //                var sf = matches[0].Groups["suffix"].Value.Trim();

        //                var delivery = uow.TransactionRepository.GetById(id, sf);

        //                if (delivery == null)
        //                {
        //                    res.HttpStatusCode = System.Net.HttpStatusCode.NotFound;
        //                    res.RequestError = $"Delivery with id: {deliveryID} cannot not be found.";
        //                }
        //                else
        //                {
        //                    if (delivery.BillOfLading == null)
        //                    {
        //                        delivery.BillOfLading = DateTime.Now;
        //                        uow.TransactionRepository.Update(delivery);

        //                        _logger.LogInformation("Bill Of Lading set to: {0}", delivery.BillOfLading);
        //                    }
        //                    else
        //                    {
        //                        _logger.LogInformation("Bill of Lading is already set to: {0}", delivery.BillOfLading);
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            uow.Rollback();
        //            throw;
        //        }
        //    }

        //    return res;
        //}

        public async Task<BaseResponse> Execute(List<string> fileNames)
        {
            var res = new BaseResponse();

            var billOfLading = DateTime.Now;

            var sheetResult = await WriteBackSheet(fileNames, billOfLading);
            if (!sheetResult.IsSuccess)
            {
                return sheetResult;
            }

            //var dbResult = await WriteBackDb(deliveryIds, billOfLading);
            //if (!dbResult.IsSuccess)
            //{
            //    return dbResult;
            //}

            return res;
        }
    }
}
