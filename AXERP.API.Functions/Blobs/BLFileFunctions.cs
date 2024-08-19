using AXERP.API.BlobHelper.ServiceContracts.Requests;
using AXERP.API.Business.Commands;
using AXERP.API.Business.Queries;
using AXERP.API.Domain;
using AXERP.API.Domain.Models;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.Domain.ServiceContracts.Responses.General;
using AXERP.API.Functions.Base;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AXERP.API.Functions.Blobs
{
    [ForSystem("Blob Storage", LogConstants.FUNCTION_BL_PROCESSING)]
    public class BLFileFunctions : BaseFunctions<BLFileFunctions>
    {
        private readonly UpdateReferencesByBlobFilesCommand _updateReferencesByBlobFilesCommand;
        private readonly ListBlobFilesQuery _listBlobFilesQuery;
        private readonly DeleteBlobFilesCommand _deleteBlobFilesCommand;
        private readonly UploadBlobFilesCommand _uploadBlobFilesCommand;
        private readonly UploadBlobFileCommand _uploadBlobFileCommand;

        public BLFileFunctions(
            AxerpLoggerFactory loggerFactory,
            ListBlobFilesQuery listBlobFilesQuery,
            DeleteBlobFilesCommand deleteBlobFilesCommand,
            UploadBlobFilesCommand uploadBlobFilesCommand,
            UploadBlobFileCommand uploadBlobFileCommand,
            UpdateReferencesByBlobFilesCommand updateReferencesByBlobFilesCommand) : base(loggerFactory)
        {
            _updateReferencesByBlobFilesCommand = updateReferencesByBlobFilesCommand;
            _listBlobFilesQuery = listBlobFilesQuery;
            _deleteBlobFilesCommand = deleteBlobFilesCommand;
            _uploadBlobFilesCommand = uploadBlobFilesCommand;
            _uploadBlobFileCommand = uploadBlobFileCommand;
        }

        [Function(nameof(ListBlobFiles))]
        [OpenApiOperation(operationId: nameof(ListBlobFiles), tags: new[] { "blob" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(BaseDataResponse<BlobFile>), Description = "The OK response")]
        public async Task<IActionResult> ListBlobFiles([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            try
            {
                SetLoggerProcessData(UserName);

                _listBlobFilesQuery.SetLoggerProcessData(UserName, id: _logger.ProcessId);
                var result = await _listBlobFilesQuery.Execute();

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
                _logger.LogError(ex, "Error while listing blob files");
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

        [Function(nameof(DeleteBlobFiles))]
        [OpenApiOperation(operationId: nameof(DeleteBlobFiles), tags: new[] { "blob" })]
        [OpenApiRequestBody("application/json", typeof(DeleteBlobFilesRequest), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(DeleteBlobfilesResponse), Description = "The OK response")]
        public async Task<IActionResult> DeleteBlobFiles([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req, [FromBody] DeleteBlobFilesRequest request)
        {
            try
            {
                SetLoggerProcessData(UserName);

                _deleteBlobFilesCommand.SetLoggerProcessData(UserName, id: _logger.ProcessId);
                var result = await _deleteBlobFilesCommand.Execute(request);

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
                _logger.LogError(ex, "Error while deleting blob files");
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

        [Function(nameof(UploadBlobFile))]
        [OpenApiOperation(operationId: nameof(UploadBlobFile), tags: new[] { "blob" })]
        //[OpenApiRequestBody("application/json", typeof(UploadBlobFilesRequest), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(BaseResponse), Description = "The OK response")]
        public async Task<IActionResult> UploadBlobFile([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req, [FromForm] IFormFile file)
        {
            try
            {
                SetLoggerProcessData(UserName);

                if (file == null)
                {
                    throw new Exception("File is null!");
                }

                BlobUploadFile bl = FormFileToBlobUploadFile(file);

                _uploadBlobFilesCommand.SetLoggerProcessData(UserName, id: _logger.ProcessId);
                var result = await _uploadBlobFileCommand.Execute(new UploadBlobFileRequest
                {
                    BlobUploadFile = bl
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
                _logger.LogError(ex, "Error while uploading blob file");
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

        [Function(nameof(ProcessBlobFiles))]
        [OpenApiOperation(operationId: nameof(ProcessBlobFiles), tags: new[] { "blob" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> ProcessBlobFiles([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        {
            try
            {
                SetLoggerProcessData(UserName);

                var blobImportFolder = EnvironmentHelper.TryGetParameter("BlobStorageImportFolder");
                var blobProcessedFolder = EnvironmentHelper.TryGetParameter("BlobStorageProcessedFolder");
                var regex = EnvironmentHelper.TryGetParameter("BlobStorePdfFileRegexPattern");

                if (string.IsNullOrWhiteSpace(blobImportFolder))
                {
                    throw new Exception("Missing parameter: BlobStorageImportFolder");
                }

                _updateReferencesByBlobFilesCommand.SetLoggerProcessData(UserName, id: _logger.ProcessId);
                var result = await _updateReferencesByBlobFilesCommand.Execute(new Domain.ServiceContracts.Requests.ProcessBlobFilesRequest
                {
                    BlobStorageImportFolder = blobImportFolder,
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

        public static BlobUploadFile FormFileToBlobUploadFile(IFormFile file)
        {
            BlobUploadFile bl;
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                fileBytes = ms.ToArray();

                var fileName = string.Empty;
                var folderName = string.Empty;

                if (file.FileName.Contains("/"))
                {
                    var parts = file.FileName.Split("/", 2);
                    fileName = parts[0];
                    folderName = parts[1];
                }
                else
                {
                    fileName = file.FileName;
                }

                bl = new BlobUploadFile
                {
                    FileName = fileName,
                    Folder = folderName,
                    Content = fileBytes
                };
            }
            return bl;
        }
    }
}
