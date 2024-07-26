using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using System.Net;
using AXERP.API.BlobHelper.Managers;
using AXERP.API.Domain.ServiceContracts.Responses;

namespace AXERP.API.Functions.Blobs
{
    public class BLFileFunctions
    {
        private readonly ILogger<BLFileFunctions> _logger;

        public BLFileFunctions(ILogger<BLFileFunctions> logger)
        {
            _logger = logger;
        }

        [Function(nameof(BLFileFunctions))]
        [OpenApiOperation(operationId: nameof(ProcessBlobFiles), tags: new[] { "blob" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> ProcessBlobFiles([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            try
            {
                var blobConnectionString = Environment.GetEnvironmentVariable("BlobStorageConnectionString");
                var blobStorageName = Environment.GetEnvironmentVariable("BlobStorageName");
                var blobImportFolder = Environment.GetEnvironmentVariable("BlobStorageImportFolder");
                var blobProcessedFolder = Environment.GetEnvironmentVariable("BlobStorageProcessedFolder");
                var regex = Environment.GetEnvironmentVariable("BlobStorePdfFileRegexPattern");

                var containerHelper = new BlobManager(blobConnectionString, blobStorageName);

                var files = await containerHelper.GetFiles(blobImportFolder, regex);
                var files2 = await containerHelper.GetFiles(blobProcessedFolder);

                return new OkObjectResult("Blob files processed!");
            }
            catch(Exception ex)
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
