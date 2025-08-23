using AXERP.API.Domain.Util;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Polly;
using GoogleCellData = Google.Apis.Sheets.v4.Data.CellData;
using AxerpCellData = AXERP.API.Domain.Models.CellData;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;

namespace AXERP.API.GoogleHelper.Managers
{
    public sealed class GoogleSheetManager : IDisposable
    {
        public const string DEFAULT_CREDENTIALS_FILENAME = "google-credentials.json";

        private readonly SheetsService _sheetsService;
        private readonly ResiliencePipeline _resiliencePipeline;
        private readonly string _spreadSheetId;

        public GoogleSheetManager(SheetsService sheetsService, string spreadSheetId, ResiliencePipeline resiliencePipeline)
        {
            _sheetsService = sheetsService;
            _spreadSheetId = spreadSheetId;
            _resiliencePipeline = resiliencePipeline;
        }

        public async Task<IList<IList<object>>> ReadGoogleSheetRawAsync(string range)
        {
            return await _resiliencePipeline.ExecuteAsync(async token =>
            {
                GetRequest getRequest = _sheetsService.Spreadsheets.Values.Get(_spreadSheetId, range);

                var getResponse = await getRequest.ExecuteAsync(token);
                IList<IList<object>> values = getResponse.Values;

                return values;
            });
        }

        public async Task<IEnumerable<string>> ReadHeaderAsync()
        {
            return await _resiliencePipeline.ExecuteAsync(async token =>
            {
                GetRequest request = _sheetsService.Spreadsheets.Values.Get(_spreadSheetId, "Deliveries!1:1");
                ValueRange range = await request.ExecuteAsync(token);

                if (range.Values.Count == 0)
                    return Enumerable.Empty<string>();

                return range.Values[0].Select(x => x.ToString() ?? string.Empty);
            });
        }

        public Task<IEnumerable<AxerpCellData>> ReadColumnAsync(int columnIndex)
        {
            return ReadColumnAsync(SheetHelperMethods.GetExcelColumnName(columnIndex));
        }

        public async Task<IEnumerable<AxerpCellData>> ReadColumnAsync(string column)
        {
            return await _resiliencePipeline.ExecuteAsync(async token =>
            {
                SpreadsheetsResource.GetRequest request = _sheetsService.Spreadsheets.Get(_spreadSheetId);
                request.IncludeGridData = true;
                request.Ranges = new[] { $"Deliveries!{column}:{column}" };

                Spreadsheet range = await request.ExecuteAsync(token);

                var rows = range.Sheets[0].Data[0].RowData
                    .SelectMany(x => x.Values is not null ? x.Values : Enumerable.Empty<GoogleCellData>())
                    .Select((cell, rowIndex) => new AxerpCellData
                    {
                        Value = cell?.FormattedValue,
                        Row = rowIndex + 1,
                    })
                    .ToArray();

                return rows;
            });
        }

        public async Task UpdateCellsAsync(IEnumerable<AxerpCellData> cells)
        {
            await _resiliencePipeline.ExecuteAsync(async token =>
            {
                BatchUpdateValuesRequest requestBody = new()
                {
                    ValueInputOption = "USER_ENTERED",
                    Data = cells.Where(x => x.Column is not null && x.Row is not null)
                        .Select(x => new ValueRange
                        {
                            Range = $"Deliveries!{SheetHelperMethods.GetExcelColumnName(x.Column!.Value)}{x.Row!.Value}",
                            Values = new[] { new[] { x.Value }},
                        })
                        .ToArray(),
                };
                BatchUpdateRequest request = _sheetsService.Spreadsheets.Values.BatchUpdate(requestBody, _spreadSheetId);

                await request.ExecuteAsync(token);
            });
        }

        public async Task<UpdateValuesResponse> UpdateCellAsync(string tabName, string columnm, int row, object data)
        {
            if (row == 1)
            {
                throw new Exception("Header row cannot be updated!");
            }
            if (row < 1)
            {
                throw new Exception("Row number is too small!");
            }

            var dataValueRange = new ValueRange();

            var tab = string.IsNullOrWhiteSpace(tabName) ? string.Empty : tabName + "!";
            var range = $"{tab}{columnm}{row}";

            dataValueRange.Range = range;
            dataValueRange.MajorDimension = "COLUMNS";

            var newData = new List<object>() { data };
            dataValueRange.Values = new List<IList<object>> { newData };

            UpdateRequest request = _sheetsService.Spreadsheets.Values.Update(dataValueRange, _spreadSheetId, range);
            request.ValueInputOption = UpdateRequest.ValueInputOptionEnum.RAW;

            var response = await request.ExecuteAsync();

            return await _resiliencePipeline.ExecuteAsync(async token => await request.ExecuteAsync(token));
        }

        public void Dispose()
        {
            _sheetsService.Dispose();
        }
    }
}