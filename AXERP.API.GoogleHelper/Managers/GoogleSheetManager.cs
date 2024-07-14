using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace AXERP.API.GoogleHelper.Managers
{
    public class GoogleSheetManager
    {
        public const string DEFAULT_CREDENTIALS_FILENAME = "google-credentials.json";

        private SheetsService _sheetsService;

        public GoogleSheetManager(string appName = "AXERP.API", string credentialsFileName = DEFAULT_CREDENTIALS_FILENAME)
        {
            GoogleCredential credential;
            using (var stream = new FileStream(credentialsFileName, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
            }
            _sheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = appName
            });
        }

        private string SheetJsonToObjectJson(IList<IList<object>> values)
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

            var dataJson = System.Text.Json.JsonSerializer.Serialize(rows);

            return dataJson;
        }

        private async Task<IList<IList<object>>> ReadGoogleSheetRaw(string spreadSheetId, string range)
        {
            SpreadsheetsResource.ValuesResource.GetRequest getRequest = _sheetsService.Spreadsheets.Values.Get(spreadSheetId, range);

            var getResponse = await getRequest.ExecuteAsync();
            IList<IList<object>> values = getResponse.Values;

            return values;
        }

        public async Task<string> ReadGoogleSheetAsJson(string spreadSheetId, string range)
        {
            var values = await ReadGoogleSheetRaw(spreadSheetId, range);

            var dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(values);

            return dataJson;
        }

        public async Task<List<T>> ReadGoogleSheet<T>(string spreadSheetId, string range)
        {
            var dataJson = SheetJsonToObjectJson(await ReadGoogleSheetRaw(spreadSheetId, range));

            var data = System.Text.Json.JsonSerializer.Deserialize<List<T>>(dataJson);

            return data;
        }
    }
}