using AXERP.API.GoogleHelper.JsonConverters;
using AXERP.API.GoogleHelper.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Newtonsoft.Json;

namespace AXERP.API.GoogleHelper.Managers
{
    public enum CredentialsFormats
    {
        None, FileName, Text
    }

    public class GoogleSheetManager
    {
        public const string DEFAULT_CREDENTIALS_FILENAME = "google-credentials.json";

        private SheetsService _sheetsService;

        public GoogleSheetManager(string appName = "AXERP.API", string credentials = DEFAULT_CREDENTIALS_FILENAME, CredentialsFormats format = CredentialsFormats.FileName)
        {
            GoogleCredential credential;
            switch (format)
            {
                case CredentialsFormats.FileName:
                    {
                        using (var stream = new FileStream(credentials, FileMode.Open, FileAccess.Read))
                        {
                            credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
                        }

                        _sheetsService = new SheetsService(new BaseClientService.Initializer()
                        {
                            HttpClientInitializer = credential,
                            ApplicationName = appName
                        });

                        break;
                    }
                case CredentialsFormats.Text:
                    {
                        using (var stream = new MemoryStream())
                        {
                            using (var writer = new StreamWriter(stream))
                            {
                                writer.Write(credentials);
                                writer.Flush();
                                stream.Position = 0;
                                credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
                            }
                        }

                        _sheetsService = new SheetsService(new BaseClientService.Initializer()
                        {
                            HttpClientInitializer = credential,
                            ApplicationName = appName
                        });

                        break;
                    }
                case CredentialsFormats.None:
                default:
                    throw new Exception("Google Sheet Service validation / initialization failed.");
            }
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

            var dataJson = JsonConvert.SerializeObject(rows);

            return dataJson;
        }

        public async Task<IList<IList<object>>> ReadGoogleSheetRaw(string spreadSheetId, string range)
        {
            SpreadsheetsResource.ValuesResource.GetRequest getRequest = _sheetsService.Spreadsheets.Values.Get(spreadSheetId, range);

            var getResponse = await getRequest.ExecuteAsync();
            IList<IList<object>> values = getResponse.Values;

            return values;
        }

        public async Task<string> ReadGoogleSheetAsJson(string spreadSheetId, string range)
        {
            var values = await ReadGoogleSheetRaw(spreadSheetId, range);

            var dataJson = JsonConvert.SerializeObject(values);

            return dataJson;
        }

        public async Task<GenericSheetImportResult<RowType>> ReadGoogleSheet<RowType>(string spreadSheetId, string range, string sheetCulture)
        {
            var raw = await ReadGoogleSheetRaw(spreadSheetId, range);
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
    }
}