using AXERP.API.Domain;
using AXERP.API.Domain.Entities;
using AXERP.API.GoogleHelper.Models;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace AXERP.API.Business.SheetProcessors
{
    [ForSystem("GoogleSheet Processing", LogConstants.FUNCTION_GOOGLE_SYNC)]
    public class GasTransactionSheetProcessor : BaseSheetProcessors<Delivery, GasTransactionSheetProcessor>
    {
        private readonly UnitOfWorkFactory _uowFactory;

        public GasTransactionSheetProcessor(AxerpLoggerFactory loggerFactory, UnitOfWorkFactory uowFactory) : base(loggerFactory)
        {
            _uowFactory = uowFactory;
        }

        public override GenericSheetImportResult<Delivery> ProcessRows(IList<IList<object>> sheet_value_range, string culture_code)
        {
            var headers = sheet_value_range[0];
            var sheet_rows = sheet_value_range.Skip(1).ToList();

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

            // Filtering range by EOD
            var eodRowIndex = sheet_rows.FindIndex(row =>
            {
                return row.Any(x => x != null && (x.ToString() ?? string.Empty).ToLower().Contains("#end"));
            });
            sheet_rows = sheet_rows.GetRange(0, eodRowIndex);

            _logger.LogInformation("EOD marker encountered at line: {0}.", eodRowIndex - 1);

            var result = new List<Delivery>();
            var errors = new List<string>();
            var invalidRows = 0;
            var totalRows = sheet_rows.Count - 1;

            // Parallel processing
            var dataChunks = sheet_rows.Chunk(100);
            var partialResults = new List<GenericSheetImportResult<Delivery>>();

            var startRowIndex = 0;
            var allDeliveryIds = new List<string>();
            Parallel.ForEach(dataChunks, dataChunk =>
            {
                var pr = _Map(field_names, dataChunk, culture_code, startRowIndex);
                partialResults.Add(pr.Item1);
                allDeliveryIds.AddRange(pr.Item2);
                startRowIndex += 100;
            });

            _logger.LogInformation("All non-empty DeliveryID from the google sheet: {0}", string.Join(", ", allDeliveryIds));

            // Merge results
            foreach (var partialResult in partialResults)
            {
                result.AddRange(partialResult.Data ?? new List<Delivery>());
                invalidRows += partialResult.InvalidRows;
                errors.AddRange(partialResult.Errors);
            }

            return new GenericSheetImportResult<Delivery>
            {
                Data = result,
                InvalidRows = invalidRows,
                TotalRowsInSheet = totalRows,
                Errors = errors
            };
        }

        private (GenericSheetImportResult<Delivery>, List<string>) _Map(Dictionary<string, int> field_names, IList<IList<object>>? sheet_rows, string culture_code, int startRowIndex)
        {
            var result = new List<Delivery>();
            var errors = new List<string>();
            var invalidRows = 0;

            var minSqlYear = 1753;

            var allDeliveryIds = new List<string>();

            for (var i = 0; i < sheet_rows.Count; i++)
            {
                var sheet_row_index = startRowIndex + i + 2;
                var row = sheet_rows[i];

                try
                {
                    if (row.Count == 0)
                    {
                        invalidRows++;
                        errors.Add($"Empty row. Row index: {sheet_row_index}");
                        continue;
                    }

                    var gasTransaction = new Delivery();
                    var field_idx = 0;

                    // MOCK, only for debug
                    //var rngArray = new bool[] { false, false, true, false, true, false, false, false, true, false, false, false, false, false };
                    //var rng = new Random();
                    //if (rngArray[rng.Next(0, rngArray.Length)])
                    //{
                    //    continue;
                    //}
                    //if (rngArray[rng.Next(0, rngArray.Length)])
                    //{
                    //    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                    //    var hash = new string(Enumerable.Repeat(chars, 10)
                    //        .Select(s => s[rng.Next(s.Length)]).ToArray());
                    //    gasTransaction.AXERPHash = chars;
                    //}
                    //else if (rngArray[rng.Next(0, rngArray.Length)])
                    //{
                    //    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                    //    var hash = new string(Enumerable.Repeat(chars, 10)
                    //        .Select(s => s[rng.Next(s.Length)]).ToArray());
                    //    gasTransaction.DeliveryID = chars;
                    //}

                    // DeliveryID
                    field_idx = field_names[nameof(gasTransaction.DeliveryID)];

                    if (row.Count <= field_idx || row[field_idx] == null || string.IsNullOrWhiteSpace(row[field_idx].ToString()))
                    {
                        invalidRows++;
                        errors.Add($"Missing Delivery ID. Row index: {sheet_row_index}");
                        continue;
                    }

                    gasTransaction.DeliveryID = row[field_idx].ToString()!;
                    allDeliveryIds.Add(gasTransaction.DeliveryID);

                    if (result.Any(x => x.DeliveryID == gasTransaction.DeliveryID))
                    {
                        invalidRows++;
                        errors.Add($"Duplicate Delivery ID: {gasTransaction.DeliveryID}. RowIndex: {sheet_row_index}");
                        continue;
                    }

                    // SalesStatus
                    field_idx = field_names[nameof(gasTransaction.SalesStatus)];
                    if (row.Count <= field_idx)
                    {
                        //result.Add(gasTransaction);

                        invalidRows++;
                        errors.Add($"Missing Sales Status for row with Delivery ID: {gasTransaction.DeliveryID}. Row index: {sheet_row_index}");

                        continue;
                    }

                    var rawSalesStatus = row[field_idx]?.ToString();
                    if (string.IsNullOrWhiteSpace(rawSalesStatus))
                    {
                        invalidRows++;
                        errors.Add($"Missing Sales Status for row with Delivery ID: {gasTransaction.DeliveryID}. Row index: {sheet_row_index}");
                        continue;
                    }

                    gasTransaction.SalesStatus = string.IsNullOrWhiteSpace(rawSalesStatus) ? null : rawSalesStatus;

                    // Status
                    field_idx = field_names[nameof(gasTransaction.Status)];
                    if (row.Count <= field_idx)
                    {
                        //result.Add(gasTransaction);

                        invalidRows++;
                        errors.Add($"Missing Status for row with Delivery ID: {gasTransaction.DeliveryID}. Row index: {sheet_row_index}");

                        continue;
                    }

                    var rawStatus = row[field_idx]?.ToString();
                    if (string.IsNullOrWhiteSpace(rawStatus))
                    {
                        invalidRows++;
                        errors.Add($"Missing Status for row with Delivery ID: {gasTransaction.DeliveryID}. Row index: {sheet_row_index}");
                        continue;
                    }

                    gasTransaction.Status = string.IsNullOrWhiteSpace(rawSalesStatus) ? null : rawSalesStatus;

                    // DateLoadedEnd
                    field_idx = field_names[nameof(gasTransaction.DateLoadedEnd)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (DateTime.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out DateTime DateLoadedEnd))
                    {
                        if (DateLoadedEnd.Year < minSqlYear)
                        {
                            invalidRows++;
                            errors.Add($"DateLoadedEnd date is too small: {DateLoadedEnd} for row with Delivery ID: {gasTransaction.DeliveryID}. Row index: {sheet_row_index}");
                            continue;
                        }
                        gasTransaction.DateLoadedEnd = DateLoadedEnd;
                    }

                    // DateDelivered
                    field_idx = field_names[nameof(gasTransaction.DateDelivered)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (DateTime.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out DateTime DateDelivered))
                    {
                        if (DateDelivered.Year < minSqlYear)
                        {
                            invalidRows++;
                            errors.Add($"DateDelivered date is too small: {DateDelivered} for row with Delivery ID: {gasTransaction.DeliveryID}. Row index: {sheet_row_index}");
                            continue;
                        }
                        gasTransaction.DateDelivered = DateDelivered;
                    }

                    // SalesContractID
                    field_idx = field_names[nameof(gasTransaction.SalesContractID)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.SalesContractID = row[field_idx]?.ToString();

                    // Terminal
                    field_idx = field_names[nameof(gasTransaction.Terminal)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.Terminal = row[field_idx]?.ToString();

                    // QtyLoaded
                    field_idx = field_names[nameof(gasTransaction.QtyLoaded)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (double.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out double QtyLoaded))
                    {
                        gasTransaction.QtyLoaded = QtyLoaded;
                    }

                    // StockDays
                    field_idx = field_names[nameof(gasTransaction.StockDays)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (int.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out int StockDays))
                    {
                        gasTransaction.StockDays = StockDays;
                    }

                    // SlotBookedByAXGTT
                    field_idx = field_names[nameof(gasTransaction.SlotBookedByAXGTT)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (int.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out int SlotBookedByAXGTT))
                    {
                        gasTransaction.SlotBookedByAXGTT = SlotBookedByAXGTT;
                    }

                    // ToDeliveryID
                    field_idx = field_names[nameof(gasTransaction.ToDeliveryID)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (long.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out long ToDeliveryID))
                    {
                        gasTransaction.ToDeliveryID = ToDeliveryID;
                    }

                    // SpecificDeliveryPoint
                    field_idx = field_names[nameof(gasTransaction.SpecificDeliveryPoint)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.SpecificDeliveryPoint = row[field_idx]?.ToString();

                    // DeliveryPoint
                    field_idx = field_names[nameof(gasTransaction.DeliveryPoint)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.DeliveryPoint = row[field_idx]?.ToString();

                    // Transporter
                    field_idx = field_names[nameof(gasTransaction.Transporter)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.Transporter = row[field_idx]?.ToString();

                    // DeliveryUP
                    field_idx = field_names[nameof(gasTransaction.DeliveryUP)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (double.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out double DeliveryUP))
                    {
                        gasTransaction.DeliveryUP = DeliveryUP;
                    }

                    // TransportCharges
                    field_idx = field_names[nameof(gasTransaction.TransportCharges)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (double.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out double TransportCharges))
                    {
                        gasTransaction.TransportCharges = TransportCharges;
                    }

                    // UnitSlotCharge
                    field_idx = field_names[nameof(gasTransaction.UnitSlotCharge)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (double.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out double UnitSlotCharge))
                    {
                        gasTransaction.UnitSlotCharge = UnitSlotCharge;
                    }

                    // ServiceCharges
                    field_idx = field_names[nameof(gasTransaction.ServiceCharges)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (double.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out double ServiceCharges))
                    {
                        gasTransaction.ServiceCharges = ServiceCharges;
                    }

                    // UnitStorageCharge
                    field_idx = field_names[nameof(gasTransaction.UnitStorageCharge)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (double.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out double UnitStorageCharge))
                    {
                        gasTransaction.UnitStorageCharge = UnitStorageCharge;
                    }

                    // StorageCharge
                    field_idx = field_names[nameof(gasTransaction.StorageCharge)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (double.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out double StorageCharge))
                    {
                        gasTransaction.StorageCharge = StorageCharge;
                    }

                    // OtherCharges
                    field_idx = field_names[nameof(gasTransaction.OtherCharges)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (double.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out double OtherCharges))
                    {
                        gasTransaction.OtherCharges = OtherCharges;
                    }

                    // Sales
                    field_idx = field_names[nameof(gasTransaction.Sales)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (double.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out double Sales))
                    {
                        gasTransaction.Sales = Sales;
                    }

                    // CMR
                    field_idx = field_names[nameof(gasTransaction.CMR)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (DateTime.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out DateTime CMR))
                    {
                        if (CMR.Year < minSqlYear)
                        {
                            invalidRows++;
                            errors.Add($"CMR date is too small: {CMR} for row with Delivery ID: {gasTransaction.DeliveryID}. Row index: {sheet_row_index}");
                            continue;
                        }
                        gasTransaction.CMR = CMR;
                    }

                    // BioMWh
                    field_idx = field_names[nameof(gasTransaction.BioMWh)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (double.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out double BioMWh))
                    {
                        gasTransaction.BioMWh = BioMWh;
                    }

                    // BillOfLading
                    field_idx = field_names[nameof(gasTransaction.BillOfLading)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    if (DateTime.TryParse(row[field_idx]?.ToString(), new CultureInfo(culture_code), out DateTime BillOfLading))
                    {
                        gasTransaction.BillOfLading = BillOfLading;
                    }

                    // BioAddendum
                    field_idx = field_names[nameof(gasTransaction.BioAddendum)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.BioAddendum = row[field_idx]?.ToString();

                    // Comment
                    field_idx = field_names[nameof(gasTransaction.Comment)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.Comment = row[field_idx]?.ToString();

                    // CustomerNote
                    field_idx = field_names[nameof(gasTransaction.CustomerNote)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.CustomerNote = row[field_idx]?.ToString();

                    // Customer
                    field_idx = field_names[nameof(gasTransaction.Customer)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.Customer = row[field_idx]?.ToString();

                    // Reference
                    field_idx = field_names[nameof(gasTransaction.Reference)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.Reference = row[field_idx]?.ToString();

                    // Reference2
                    field_idx = field_names[nameof(gasTransaction.Reference2)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.Reference2 = row[field_idx]?.ToString();

                    // Reference3
                    field_idx = field_names[nameof(gasTransaction.Reference3)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.Reference3 = row[field_idx]?.ToString();

                    // TruckLoadingCompanyComment
                    field_idx = field_names[nameof(gasTransaction.TruckLoadingCompanyComment)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.TruckLoadingCompanyComment = row[field_idx]?.ToString();

                    // TruckCompany
                    field_idx = field_names[nameof(gasTransaction.TruckCompany)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.TruckCompany = row[field_idx]?.ToString();

                    // AXERP Hash
                    field_idx = field_names[nameof(gasTransaction.AXERPHash)];
                    if (row.Count <= field_idx)
                    {
                        result.Add(gasTransaction);
                        continue;
                    }

                    gasTransaction.AXERPHash = row[field_idx]?.ToString();

                    // Add to result

                    result.Add(gasTransaction);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception while processing row with index: {sheet_row_index}.");
                    errors.Add($"Exception while processing row with index: {sheet_row_index}. Error: " + ex.Message);
                    invalidRows++;
                }
            }

            var importResult = new GenericSheetImportResult<Delivery>
            {
                Data = result,
                InvalidRows = invalidRows,
                TotalRowsInSheet = sheet_rows.Count(),
                Errors = errors
            };

            return (importResult, allDeliveryIds);
        }
    }
}
