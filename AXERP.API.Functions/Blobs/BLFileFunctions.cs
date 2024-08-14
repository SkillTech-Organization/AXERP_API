using AXERP.API.Business.Commands;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.LogHelper.Factories;
using AXERP.API.LogHelper.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AXERP.API.Functions.Blobs
{
    public class BLFileFunctions
    {
        private readonly AxerpLogger<BLFileFunctions> _logger;
        private readonly UpdateReferencesByBlobFilesCommand _updateReferencesByBlobFilesCommand;

        private string userName = "Unknown";

        public BLFileFunctions(AxerpLoggerFactory loggerFactory, UpdateReferencesByBlobFilesCommand updateReferencesByBlobFilesCommand)
        {
            _logger = loggerFactory.Create<BLFileFunctions>();
            _updateReferencesByBlobFilesCommand = updateReferencesByBlobFilesCommand;
        }

        [Function(nameof(ProcessBlobFiles))]
        [OpenApiOperation(operationId: nameof(ProcessBlobFiles), tags: new[] { "blob" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> ProcessBlobFiles([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            try
            {
                _logger.Set(user: userName, system: "AXERP.API");

                var blobConnectionString = Environment.GetEnvironmentVariable("BlobStorageConnectionString");
                var blobStorageName = Environment.GetEnvironmentVariable("BlobStorageName");
                var blobImportFolder = Environment.GetEnvironmentVariable("BlobStorageImportFolder");
                var blobProcessedFolder = Environment.GetEnvironmentVariable("BlobStorageProcessedFolder");
                var regex = Environment.GetEnvironmentVariable("BlobStorePdfFileRegexPattern");

                _updateReferencesByBlobFilesCommand.SetupLogger("Unknown", _logger.ProcessId);
                var result = await _updateReferencesByBlobFilesCommand.Execute(new Domain.ServiceContracts.Requests.ProcessBlobFilesRequest
                {
                    BlobStorageConnectionString = blobConnectionString,
                    BlobStorageImportFolder = blobImportFolder,
                    BlobStorageName = blobStorageName,
                    BlobStorageProcessedFolder = blobProcessedFolder,
                    BlobStorePdfFileRegexPattern = regex
                });

                if (result.IsSuccess)
                {
                    return new OkObjectResult(result);
                }
                else
                {
                    return new ObjectResult(result)
                    {
                        StatusCode = (int?)result.HttpStatusCode
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing blob files");
                var res = new ObjectResult(new BaseResponse
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError,
                    RequestError = ex.Message
                })
                {
                    StatusCode = 500
                };
                return res;
            }
        }
    }
}
