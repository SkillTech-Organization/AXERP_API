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
using HttpMultipartParser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

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
        private readonly DownLoadBlobFileCommand _getBlobFileCommand;
        private readonly UpdateBillOfLadingCommand _updateBillOfLadingCommand;

        public const string PATH_PARAM_UPLOAD = "path";

        public BLFileFunctions(
            AxerpLoggerFactory loggerFactory,
            ListBlobFilesQuery listBlobFilesQuery,
            DeleteBlobFilesCommand deleteBlobFilesCommand,
            UploadBlobFilesCommand uploadBlobFilesCommand,
            UploadBlobFileCommand uploadBlobFileCommand,
            UpdateReferencesByBlobFilesCommand updateReferencesByBlobFilesCommand,
            DownLoadBlobFileCommand getBlobFileCommand,
            UpdateBillOfLadingCommand updateBillOfLadingCommand) : base(loggerFactory)
        {
            _updateReferencesByBlobFilesCommand = updateReferencesByBlobFilesCommand;
            _listBlobFilesQuery = listBlobFilesQuery;
            _deleteBlobFilesCommand = deleteBlobFilesCommand;
            _uploadBlobFilesCommand = uploadBlobFilesCommand;
            _uploadBlobFileCommand = uploadBlobFileCommand;
            _getBlobFileCommand = getBlobFileCommand;
            _updateBillOfLadingCommand = updateBillOfLadingCommand;
        }

        /// <summary>
        /// Lists all unprocessed files from the blob storage folder used for importing files.
        /// </summary>
        [Function(nameof(ListBlobFiles))]
        [OpenApiOperation(operationId: nameof(ListBlobFiles), tags: new[] { "blob" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(BaseDataResponse<BlobFile>), Description = "The OK response")]
        public async Task<IActionResult> ListBlobFiles([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            try
            {
                SetLoggerProcessData(req);

                _listBlobFilesQuery.SetLoggerProcessData(UserName, id: _logger.ProcessId);
                var result = await _listBlobFilesQuery.Execute(new Domain.ServiceContracts.Requests.ListBlobFilesQueryRequest
                {
                    ExcludeFolders = new List<string> { EnvironmentHelper.TryGetParameter("BlobStorageProcessedFolder") }
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

        /// <summary>
        /// Deletes the specified blob files from the blob storage.
        /// </summary>
        [Function(nameof(DeleteBlobFiles))]
        [OpenApiOperation(operationId: nameof(DeleteBlobFiles), tags: new[] { "blob" })]
        [OpenApiRequestBody("application/json", typeof(DeleteBlobFilesRequest), Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(DeleteBlobfilesResponse), Description = "The OK response")]
        public async Task<IActionResult> DeleteBlobFiles([HttpTrigger(AuthorizationLevel.Anonymous, "delete")] HttpRequestData req, [FromBody] DeleteBlobFilesRequest request)
        {
            try
            {
                SetLoggerProcessData(req);

                if (req == null || request == null)
                {
                    throw new Exception("Request is null!");
                }

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

        /// <summary>
        /// Uploads the provided file to blob storage.
        /// Both path (including requested destination folder) and the files are multipart/form-data parameters ("file" and "path").
        /// </summary>
        [Function(nameof(UploadBlobFile))]
        [OpenApiOperation(operationId: nameof(UploadBlobFile), tags: new[] { "blob" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(BaseResponse), Description = "The OK response")]
        public async Task<IActionResult> UploadBlobFile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "UploadBlobFile")] HttpRequestData req)
        {
            try
            {
                SetLoggerProcessData(req);

                if (req == null)
                {
                    throw new Exception("Request is null!");
                }

                // get form-body        
                var parsedFormBody = MultipartFormDataParser.ParseAsync(req.Body);
                
                if (parsedFormBody.Result.Files.Count == 0)
                {
                    throw new Exception("File is missing from the request!");
                }

                var file = parsedFormBody.Result.Files[0];
                
                var name = file.FileName;

                if (parsedFormBody.Result.Parameters.Count > 1)
                {
                    throw new Exception($"Too many parameters! Only a '{PATH_PARAM_UPLOAD}' parameter can be provided alongside the file!");
                }

                if (parsedFormBody.Result.Parameters.Count == 1 && parsedFormBody.Result.Parameters[0].Name != PATH_PARAM_UPLOAD)
                {
                    throw new Exception($"Invalid parameter! Only a '{PATH_PARAM_UPLOAD}' parameter can be provided alongside the file!");
                }

                if (parsedFormBody.Result.Parameters.Count == 1 && parsedFormBody.Result.Parameters[0].Name == PATH_PARAM_UPLOAD)
                {
                    name = parsedFormBody.Result.Parameters.Single().Data;
                }

                BlobUploadFile bl = FormFileToBlobUploadFile(file, name);

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

        /// <summary>
        /// Process blob files from the blob storage folder used for importing blob files.
        /// The files are moved between folders, so they will be deleted from the source folder.
        /// </summary>
        [Function(nameof(ProcessBlobFiles))]
        [OpenApiOperation(operationId: nameof(ProcessBlobFiles), tags: new[] { "blob" })]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> ProcessBlobFiles([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            try
            {
                SetLoggerProcessData(req);

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

        public static BlobUploadFile FormFileToBlobUploadFile(FilePart file, string? nameOverride = null)
        {
            BlobUploadFile bl;
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                file.Data.CopyTo(ms);
                fileBytes = ms.ToArray();

                var fileName = string.Empty;
                var folderName = string.Empty;

                var rawFileName = nameOverride ?? file.FileName;

                if (rawFileName.Contains("/"))
                {
                    var parts = rawFileName.Split("/", 2);
                    fileName = parts[1];
                    folderName = parts[0];
                }
                else
                {
                    fileName = rawFileName;
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

        /// <summary>
        /// Downloads a processed file from the blob storage.
        /// </summary>
        [Function(nameof(DownloadBlFile))]
        [OpenApiOperation(operationId: nameof(DownloadBlFile), tags: new[] { "customer-ui" }, Description = "Downloads a blob file by name. Only processed blob files can be downloaded.")]
        [OpenApiParameter(name: "FileName", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "Name of the processed BL file.")]
        [OpenApiParameter(name: "DeliveryID", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "Associated Delivery ID")]
        public async Task<HttpResponseData> DownloadBlFile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            SetLoggerProcessData(req);

            _logger.LogInformation("Downloading BL file...");
            _logger.LogInformation("Checking parameters...");

            var fileName = req.Query["FileName"] ?? string.Empty;
            var deliveryId = req.Query["DeliveryID"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogError("Parameter FileName is required!");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.WriteString("Parameter FileName is required!");
                return response;
            }

            if (string.IsNullOrWhiteSpace(deliveryId))
            {
                _logger.LogError("Parameter DeliveryID is required!");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.WriteString("Parameter DeliveryID is required!");
                return response;
            }

            _logger.LogInformation("FileName: {0}. Delivery ID: {1}", fileName, deliveryId);

            try
            {
                _getBlobFileCommand.SetLoggerProcessData(UserName, id: _logger.ProcessId);
                var resp = await _getBlobFileCommand.Execute(new Domain.ServiceContracts.Requests.Blob.DownloadBlobFileRequest
                {
                    FilePath = $"{EnvironmentHelper.TryGetParameter("BlobStorageProcessedFolder")}/{fileName}"
                });

                if (resp.Errors.Any())
                {
                    var msg = string.Join(", ", resp.Errors);

                    _logger.LogError("Blob file download failed. {0}", msg);

                    var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                    response.WriteString(msg);
                    return response;
                }
                else
                {
                    _logger.LogInformation("Blob file download finished.");

                    var response = req.CreateResponse(HttpStatusCode.OK);
                    response.WriteBytes(resp.FileContent);
                    response.Headers.Add("Content-Type", "application/octet-stream");
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while downloading BL file");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.WriteString("Error while downloading BL file: " + ex.Message);
                return response;
            }
        }
    }
}
