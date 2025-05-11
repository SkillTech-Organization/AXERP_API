using AXERP.API.GoogleHelper.JsonConverters;
using AXERP.API.GoogleHelper.Models;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
using Polly;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;

namespace AXERP.API.GoogleHelper.Managers
{
    public sealed class GoogleSheetManager : IDisposable
    {
        public const string DEFAULT_CREDENTIALS_FILENAME = "google-credentials.json";

        private readonly SheetsService _sheetsService;
        private readonly ResiliencePipeline _resiliencePipeline;

        public GoogleSheetManager(SheetsService sheetsService, ResiliencePipeline resiliencePipeline)
        {
            _sheetsService = sheetsService;
            _resiliencePipeline = resiliencePipeline;
        }

        private static string SheetJsonToObjectJson(IList<IList<object>> values)
        {
            var _keys = values[0];
            var _values = values.Skip(1).ToList();

            var rows = new List<Dictionary<string, object>>();
            for (int i = 0; i < _values.Count; i++)
            {
                var row = new Dictionary<string, object>();
                for (int key_idx = 0; key_idx < _keys.Count; key_idx++)
                {
                    var key = _keys[key_idx].ToString();

                    // Empty trailing columns are omitted so indexes must be checked
                    if (_values[i].Count > key_idx)
                    {
                        row[key] = _values[i][key_idx];
                    }
                }
                rows.Add(row);
            }

            var dataJson = JsonConvert.SerializeObject(rows);

            return dataJson;
        }

        public async Task<IList<IList<object>>> ReadGoogleSheetRawAsync(string spreadSheetId, string range)
        {
            return await _resiliencePipeline.ExecuteAsync(async token =>
            {
                GetRequest getRequest = _sheetsService.Spreadsheets.Values.Get(spreadSheetId, range);

                var getResponse = await getRequest.ExecuteAsync(token);
                IList<IList<object>> values = getResponse.Values;

                return values;
            });
        }

        public async Task<string> ReadGoogleSheetAsJsonAsync(string spreadSheetId, string range)
        {
            var values = await ReadGoogleSheetRawAsync(spreadSheetId, range);

            var dataJson = JsonConvert.SerializeObject(values);

            return dataJson;
        }

        public async Task<GenericSheetImportResult<RowType>> ReadGoogleSheetAsync<RowType>(string spreadSheetId, string range, string sheetCulture)
        {
            var raw = await ReadGoogleSheetRawAsync(spreadSheetId, range);
            var dataJson = SheetJsonToObjectJson(raw);

            var result = new GenericSheetImportResult<RowType>
            {
                Data = new List<RowType>(),
                Errors = new List<string>(),
                InvalidRows = 0,
                TotalRowsInSheet = raw.Count - 1 // First row is header so it doesn't count
            };

            result.Data = JsonConvert.DeserializeObject<List<RowType>>(dataJson, new JsonSerializerSettings
            {
                Culture = new System.Globalization.CultureInfo(sheetCulture),
                Converters = new List<JsonConverter>
                {
                    new DoubleConverter(),
                    new LongConverter()
                },
                Error = (obj, args) =>
                {
                    var error = args.ErrorContext;

                    result.InvalidRows++;
                    result.Errors.Add(error.Error.Message);

                    error.Handled = true;
                }
            }) ?? new List<RowType>();

            return result;
        }

        public UpdateValuesResponse UpdateCell(string spreadSheetId, string tab, string columnm, int row, object data)
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

            var _tab = string.IsNullOrWhiteSpace(tab) ? string.Empty : tab + "!";
            var range = $"{_tab}{columnm}{row}";

            dataValueRange.Range = range;
            dataValueRange.MajorDimension = "COLUMNS";

            var newData = new List<object>() { data };
            dataValueRange.Values = new List<IList<object>> { newData };

            var request = _sheetsService.Spreadsheets.Values.Update(dataValueRange, spreadSheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

            var response = request.Execute();

            return response;
        }

        public BatchUpdateValuesResponse UpdateData(string spreadSheetId, string range, List<IList<object>> data)
        {
            var updateData = new List<ValueRange>();

            var dataValueRange = new ValueRange();
            dataValueRange.Range = range;
            dataValueRange.Values = data;
            
            updateData.Add(dataValueRange);

            var requestBody = new BatchUpdateValuesRequest();
            requestBody.ValueInputOption = "USER_ENTERED";
            requestBody.Data = updateData;

            var request = _sheetsService.Spreadsheets.Values.BatchUpdate(requestBody, spreadSheetId);

            var response = request.Execute();

            return response;
        }

        public void Dispose()
        {
            _sheetsService.Dispose();
        }
    }
}