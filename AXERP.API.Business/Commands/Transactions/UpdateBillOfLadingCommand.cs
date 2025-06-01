using AXERP.API.Domain;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.ServiceContracts.Responses.Base;
using AXERP.API.Domain.Util;
using AXERP.API.GoogleHelper;
using AXERP.API.GoogleHelper.Managers;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AXERP.API.Business.Commands
{
    [ForSystem("SQL Server", LogConstants.FUNCTION_GOOGLE_SYNC)]
    public partial class UpdateBillOfLadingCommand : BaseAuditedClass<UpdateBillOfLadingCommand>
    {
        private readonly UnitOfWorkFactory _uowFactory;
        private readonly GoogleSheetManagerFactory _googleSheetManagerFactory;


        [GeneratedRegex("(?<id>[0-9]+)(?<suffix>[^0-9]{0,})", RegexOptions.IgnoreCase, "hu-HU")]
        private static partial Regex DeliveryIdRegex();

        public UpdateBillOfLadingCommand(
            AxerpLoggerFactory axerpLoggerFactory,
            UnitOfWorkFactory uowFactory,
            GoogleSheetManagerFactory googleSheetManagerFactory)
            : base(axerpLoggerFactory)
        {
            _uowFactory = uowFactory;
            _googleSheetManagerFactory = googleSheetManagerFactory;
        }

        private async Task<(List<string>, DateTime)> UpdateSheetBillOfLadingsAsync(GoogleSheetManager sheetService, List<string> fileNames, DateTime billOfLading, IList<IList<object>> rows)
        {
            // Result
            var ids = new List<string>();
            var bolDate = billOfLading;

            // Env
            var regexPattern = EnvironmentHelper.TryGetParameter("BlobStorePdfFileRegexPattern");
            var regexReferenceKey = EnvironmentHelper.TryGetParameter("RegexReferenceKey");
            var tabName = EnvironmentHelper.TryGetParameter("BulkDeliveriesSheetDataGasTransactionsTab");

            // Preprocess
            var headers = rows[0];

            // Eg. DeliveryID -> 0 (indexof Delivery ID in sheet headers)
            var fieldNames = SheetHelperMethods.GetFieldNamesWithOrder<Delivery>(headers);

            var sheetRows = rows.Skip(1).ToList();
            var sheetBillOfLadingColumn = SheetHelperMethods.GetExcelColumnName(fieldNames[nameof(Delivery.BillOfLading)] + 1);

            //var billOfLadingFormatted = billOfLading.ToString("G", new CultureInfo(sheetCulture));
            var billOfLadingFormatted = billOfLading.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

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

            sheetRows = SheetHelperMethods.UntilEndOfData(sheetRows, out int eodRowIndex);
            _logger.LogInformation("EOD marker encountered at line: {0}.", eodRowIndex - 1);

            int deliveryId = fieldNames[nameof(Delivery.DeliveryID)];
            int refBoL = fieldNames[nameof(Delivery.BillOfLading)];
            int ref1Idx = fieldNames[nameof(Delivery.Reference)];
            int ref2Idx = fieldNames[nameof(Delivery.Reference2)];
            int ref3Idx = fieldNames[nameof(Delivery.Reference3)];

            // Updating cells
            for (int rowIndex = 0; rowIndex < sheetRows.Count; rowIndex++)
            {
                var row = sheetRows[rowIndex];

                if (row.Count == 0)
                    continue;

                var transaction_id = row[deliveryId]?.ToString();

                int sheetRowNumber = rowIndex + 2;

                if (!(row.Count <= refBoL || row[refBoL] == null || string.IsNullOrWhiteSpace(row[refBoL].ToString()) || row[refBoL].ToString() == "N/A"))
                {
                    // Már ki van töltve, nem változtatjuk meg.
                    continue;
                }

                if (!(row.Count <= ref1Idx || row[ref1Idx] == null || string.IsNullOrWhiteSpace(row[ref1Idx].ToString())))
                {
                    var rawRef1 = row[ref1Idx].ToString()!.Trim();
                    if (!string.IsNullOrWhiteSpace(rawRef1) && blFileReferences.Contains(rawRef1))
                    {
                        var result = await sheetService.UpdateCellAsync(tabName, sheetBillOfLadingColumn, sheetRowNumber, billOfLadingFormatted);
                        blFileReferences.Remove(rawRef1);
                        ids.Add(transaction_id);
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
                else if (!(row.Count <= ref2Idx || row[ref2Idx] == null || string.IsNullOrWhiteSpace(row[ref2Idx].ToString())))
                {
                    var rawRef1 = row[ref2Idx].ToString()!.Trim();
                    if (!string.IsNullOrWhiteSpace(rawRef1) && blFileReferences.Contains(rawRef1))
                    {
                        var result = await sheetService.UpdateCellAsync(tabName, sheetBillOfLadingColumn, sheetRowNumber, billOfLadingFormatted);
                        blFileReferences.Remove(rawRef1);
                        ids.Add(transaction_id);
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
                else if (!(row.Count <= ref3Idx || row[ref3Idx] == null || string.IsNullOrWhiteSpace(row[ref3Idx].ToString())))
                {
                    var rawRef1 = row[ref3Idx].ToString()!.Trim();
                    if (!string.IsNullOrWhiteSpace(rawRef1) && blFileReferences.Contains(rawRef1))
                    {
                        var result = await sheetService.UpdateCellAsync(tabName, sheetBillOfLadingColumn, sheetRowNumber, billOfLadingFormatted);
                        blFileReferences.Remove(rawRef1);
                        ids.Add(transaction_id);
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

            return (ids, bolDate);
        }

        private async Task<BaseResponse> WriteBackSheet(List<string> fileNames, DateTime billOfLading)
        {
            var res = new BaseResponse();

            var tab_name = EnvironmentHelper.TryGetParameter("BulkDeliveriesSheetDataGasTransactionsTab");
            var range = EnvironmentHelper.TryGetOptionalParameter("BulkDeliveriesSheetDataGasTransactionRange");

            using GoogleSheetManager sheetService = _googleSheetManagerFactory.Create();

            var rows = await sheetService.ReadGoogleSheetRawAsync($"{tab_name}{(range?.Length > 0 ? "!" : "")}{range}");

            var bolResult = await UpdateSheetBillOfLadingsAsync(sheetService, fileNames, billOfLading, rows);

            WriteBackDatabase(bolResult.Item1, bolResult.Item2);

            return res;
        }

        public async Task<BaseResponse> ExecuteAsync(List<string> fileNames)
        {
            var res = new BaseResponse();

            var billOfLading = DateTime.Now;

            var sheetResult = await WriteBackSheet(fileNames, billOfLading);
            if (!sheetResult.IsSuccess)
            {
                return sheetResult;
            }

            return res;
        }

        private void WriteBackDatabase(List<string> ids, DateTime bol)
        {
            using (var uow = _uowFactory.Create())
            {
                var rows = uow.TransactionRepository.GetAll();
                var filtered = rows
                    .Where(x => ids.Contains(x.ID + x.IDSffx));

                foreach (var row in filtered)
                {
                    row.BillOfLading = bol;
                }

                uow.TransactionRepository.Update(filtered);
            }
        }
    }
}
