using AXERP.API.GoogleHelper.Managers;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _configuration;

    public GoogleSheetManagerFactory(ResiliencePipelineProvider<string> provider, IConfiguration configuration)
    {
        _pipeline = provider.GetPipeline(PipelineName);
        _configuration = configuration;
    }

    public GoogleSheetManager Create()
    {
        GoogleCredential credential = GetCredential();

        SheetsService sheetsService = new(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "AXERP.API"
        });

        int timeout = _configuration.GetValue<int>("GSTimeoutHandlerInSeconds");

        sheetsService.HttpClient.Timeout = TimeSpan.FromSeconds(timeout);

        return new GoogleSheetManager(sheetsService, _pipeline);
    }

    private GoogleCredential GetCredential()
    {
        string credentialsJson = _configuration.GetValue<string>("GoogleCredentials") ?? throw new InvalidOperationException("Invalid Google credentials in configuration");

        var format = CredentialsFormats.Text;
        switch (format)
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
