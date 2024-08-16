using AXERP.API.BlobHelper.ServiceContracts.Requests;
using AXERP.API.BlobHelper.ServiceContracts.Responses;
using AXERP.API.Domain;
using AXERP.API.Domain.Models;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.RegularExpressions;

namespace AXERP.API.BlobHelper.Managers
{
    [ForSystem("Blob Storage", LogConstants.FUNCTION_BL_PROCESSING)]
    public class BlobManager : BaseAuditedClass<BlobManager>
    {
        private readonly string ConnectionString = string.Empty;
        private readonly string CurrentStorage = string.Empty;

        private readonly BlobContainerClient Container;

        public BlobManager(
            AxerpLoggerFactory axerpLoggerFactory,
            string connectionString, string storageName) : base(axerpLoggerFactory)
        {
            ConnectionString = connectionString;
            CurrentStorage = storageName;
            Container = new BlobContainerClient(ConnectionString, CurrentStorage);
        }

        private bool CheckError(Response? response, out string errorMessage)
        {
            errorMessage = "Unknown Azure Blob Error";
            if (response == null)
            {
                return true;
            }
            else if (response.IsError)
            {
                if (!string.IsNullOrWhiteSpace(response.ReasonPhrase))
                {
                    errorMessage = response.ReasonPhrase;
                }
                return true;
            }
            return false;
        }

        public async Task<List<BlobFile>> ListFiles(string? folderNameFilter = null)
        {
            var result = new List<BlobFile>();

            _logger.LogInformation("Getting files from blob storage");

            if (!string.IsNullOrWhiteSpace(folderNameFilter))
            {
                _logger.LogInformation("Filter by folder name: {0}", folderNameFilter);
            }

            await foreach (var blob in Container.GetBlobsByHierarchyAsync())
            {
                if (blob.IsBlob)
                {

                    _logger.LogInformation("Blob: {0}", blob.Blob.Name);

                    var path = blob.Blob.Name;

                    var fileName = string.Empty;
                    var folderName = string.Empty;

                    if (path.Contains("/"))
                    {
                        var path_parts = path.Split("/", 2);
                        fileName = path_parts[1];
                        folderName = path_parts[0];
                    }

                    if (!string.IsNullOrWhiteSpace(folderNameFilter) && folderName != folderNameFilter)
                    {
                        continue;
                    }

                    result.Add(new BlobFile
                    {
                        FileName = fileName,
                        Folder = folderName
                    });
                }
            }

            return result;
        }

        public async Task<DeleteBlobfilesResponse> DeleteFiles(List<BlobFile> files)
        {
            var response = new DeleteBlobfilesResponse
            {
                Deleted = new List<BlobFile>(),
                NotDeleted = new List<BlobFile>(),
                Errors = new List<string>()
            };

            _logger.LogInformation("Deleting files from blob storage. Paths: {0}", string.Join(", ", files.Select(x => x.Path)));

            foreach (var file in files)
            {
                var path = file.Path;

                _logger.LogInformation("Requesting: {0}", path);

                var sourceBlob = Container.GetBlobClient(path);

                _logger.LogInformation("Deleting: {0}", path);

                var deleteResponse = await sourceBlob.DeleteAsync();

                if (CheckError(deleteResponse, out string msg))
                {
                    response.NotDeleted.Add(file);
                    var errorMsg = $"Could not delete: {path}. Error: {msg}";
                    response.Errors.Add(errorMsg);
                    _logger.LogError(errorMsg);
                    continue;
                }

                response.Deleted.Add(file);

                _logger.LogInformation("Blob successfully deleted!");
            }

            return response;
        }

        public async Task<UploadBlobfilesResponse> UploadFiles(List<BlobUploadFile> files)
        {
            var response = new UploadBlobfilesResponse
            {
                Uploaded = new List<BlobFile>(),
                NotUploaded = new List<BlobFile>(),
                Errors = new List<string>()
            };

            _logger.LogInformation("Uploading files to blob storage. Paths: {0}", string.Join(", ", files.Select(x => x.Path)));

            foreach (var file in files)
            {
                var path = file.Path;

                _logger.LogInformation("Deleting: {0}", path);

                var uploadResponse = await Container.UploadBlobAsync(path,file.Content);

                var _response = uploadResponse?.GetRawResponse();
                if (CheckError(_response, out string msg))
                {
                    response.NotUploaded.Add(file);
                    var errorMsg = $"Could not upload: {path}. Error: {msg}";
                    response.Errors.Add(errorMsg);
                    _logger.LogError(errorMsg);
                    continue;
                }

                response.Uploaded.Add(file);

                _logger.LogInformation("Blob successfully uploaded!");
            }

            return response;
        }

        public async Task<GetBlobFilesResponse> GetFiles(string? folderName = null, string? regexPattern = null)
        {
            var response = new GetBlobFilesResponse
            {
                Data = new List<GetBlobFilesItem>()
            };

            var result = new List<GetBlobFilesItem>();

            _logger.LogInformation("Getting files from folder '{0}' with regex: {1}", folderName, regexPattern ?? "NO REGEX FILTER USED");

            await foreach (var blob in Container.GetBlobsByHierarchyAsync(prefix: folderName))
            {
                if (blob.IsBlob)
                {

                    _logger.LogInformation("Checking blob for folder: {0}", blob.Blob.Name);

                    if (!blob.Blob.Name.Contains($"{folderName}/"))
                    {
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(regexPattern))
                    {
                        _logger.LogInformation("Checking blob for regex: {0}", blob.Blob.Name);

                        var matches = Regex.Matches(blob.Blob.Name, regexPattern, RegexOptions.IgnoreCase);
                        if (matches.Count == 0)
                        {
                            continue;
                        }
                        else
                        {
                            _logger.LogInformation("Matching blob found: {0}", blob.Blob.Name);

                            result.Add(new GetBlobFilesItem
                            {
                                BlobItem = blob,
                                RegexExpression = regexPattern,
                                Matches = matches.ToList()
                            });
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Blob found: {0}", blob.Blob.Name);

                        result.Add(new GetBlobFilesItem
                        {
                            BlobItem = blob
                        });
                    }
                }
            }

            response.Data = result;

            return response;
        }

        public async Task MoveFile(BlobHierarchyItem blob, string destinationName, string destinationFolder)
        {
            _logger.LogInformation("Moving blob file '{blob_name}' to folder '{dst}' with name '{name}'", blob.Blob.Name, destinationFolder, destinationName);

            var sourceBlob = Container.GetBlobClient(blob.Blob.Name);
            var destinationBlob = Container.GetBlobClient($"{destinationFolder}/{destinationName}");

            _logger.LogInformation("Copying between folders...");

            var copyResponse = await destinationBlob.StartCopyFromUriAsync(sourceBlob.Uri);

            if (copyResponse.HasValue)
            {
                throw new Exception(copyResponse.GetRawResponse().ReasonPhrase);
            }

            _logger.LogInformation("Deleting from original folder...");

            var deleteResponse = await sourceBlob.DeleteAsync();

            if (CheckError(deleteResponse, out string errorMessage))
            {
                throw new Exception(errorMessage);
            }

            _logger.LogInformation("Blob successfully moved to other folder!");
        }
    }
}
