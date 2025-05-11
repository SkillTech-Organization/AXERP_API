using AXERP.API.GoogleHelper.Managers;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Polly;
using Polly.Registry;

namespace AXERP.API.GoogleHelper;

public sealed class GoogleSheetManagerFactory
{
    public const string PipelineName = "google resiliency pipeline";

    public enum CredentialsFormats
    {
        None, FileName, Text
    }

    private readonly ResiliencePipeline _pipeline;

    public GoogleSheetManagerFactory(ResiliencePipelineProvider<string> provider)
    {
        _pipeline = provider.GetPipeline(PipelineName);
    }

    public GoogleSheetManager Create()
    {
        const string appName = "AXERP.API";

        SheetsService sheetsService;

        GoogleCredential credential = GetCredential();

        sheetsService = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = appName
        });

        return new GoogleSheetManager(sheetsService, _pipeline);
    }

    private static GoogleCredential GetCredential()
    {
        const string Key = "GoogleCredentials";
        string? credentialsJson = Environment.GetEnvironmentVariable(Key) ?? throw new Exception("Missing parameter: " + Key);

        switch (CredentialsFormats.Text)
        {
            case CredentialsFormats.FileName:
            {
                using (var stream = new FileStream(credentialsJson, FileMode.Open, FileAccess.Read))
                {
                    return GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
                }
            }
            case CredentialsFormats.Text:
            {
                using (var stream = new MemoryStream())
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(credentialsJson);
                        writer.Flush();
                        stream.Position = 0;
                        return GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
                    }
                }
            }
            case CredentialsFormats.None:
            default:
                throw new Exception("Google Sheet Service validation / initialization failed.");
        }
    }
}
