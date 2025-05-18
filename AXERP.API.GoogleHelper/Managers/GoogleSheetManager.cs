using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Polly;
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