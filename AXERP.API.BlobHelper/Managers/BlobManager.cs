using AXERP.API.BlobHelper.ServiceContracts.Requests;
using AXERP.API.BlobHelper.ServiceContracts.Responses;
using AXERP.API.Domain;
using AXERP.API.Domain.Models;
using AXERP.API.Domain.ServiceContracts.Responses;
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

        private bool CheckError(Response<BlobDownloadResult>? response, out string errorMessage)
        {
            var resp = response?.GetRawResponse();
            errorMessage = "Unknown Azure Blob Error";
            if (response == null || resp == null)
            {
                return true;
            }
            else if (resp.IsError)
            {
                if (!string.IsNullOrWhiteSpace(resp.ReasonPhrase))
                {
                    errorMessage = resp.ReasonPhrase;
                }
                return true;
            }
            return false;
        }

        public async Task<List<BlobFile>> ListFiles(string? folderNameFilter = null, List<string>? excludeFolders = null)
        {
            var result = new List<BlobFile>();

            _logger.LogTrace("Getting files from blob storage");

            if (!string.IsNullOrWhiteSpace(folderNameFilter))
            {
                _logger.LogTrace("Filter by folder name: {0}", folderNameFilter);
            }

            var _excludeFolders = excludeFolders ?? new List<string>();
            if (excludeFolders != null && excludeFolders.Count > 0)
            {
                _logger.LogTrace("Excluding folders: {0}", string.Join(", ", _excludeFolders));
            }

            await foreach (var blob in Container.GetBlobsByHierarchyAsync())
            {
                if (blob.IsBlob)
                {
                    var path = blob.Blob.Name;
                    if (_excludeFolders.Any(x => path.Contains(x + "/")))
                    {
                        continue;
                    }

                    _logger.LogTrace("Blob: {0}", path);

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

        public async Task<DownloadBlobFileResponse> DownloadFile(string path)
        {
            var response = new DownloadBlobFileResponse
            {
                Errors = new List<string>()
            };

            var sourceBlob = Container.GetBlobClient(path);
            
            var doExist = await sourceBlob.ExistsAsync();
            if (!doExist.Value)
            {
                var errorMsg = $"Could not download blob file. Blob does not exist!";
                response.Errors.Add(errorMsg);
                _logger.LogError(errorMsg);
                return response;
            }

            var downloadResponse = await sourceBlob.DownloadContentAsync();

            if (CheckError(downloadResponse, out string msg))
            {
                var errorMsg = $"Could not download blob file. Error: {msg}";
                response.Errors.Add(errorMsg);
                _logger.LogError(errorMsg);
                return response;
            }

            response.FileContent = downloadResponse.Value.Content.ToArray();
            response.FileName = path.Contains("/") ? path.Split("/").Last() : path;

            return response;
        }

        public async Task<DeleteBlobfilesResponse> DeleteFiles(List<BlobFile> files)
        {
            var response = new DeleteBlobfilesResponse
            {
                Deleted = new List<BlobFile>(),
                NotDeleted = new List<BlobFile>(),
                Errors = new List<string>()
            };

            _logger.LogTrace("Deleting files from blob storage. Paths: {0}", string.Join(", ", files.Select(x => x.Path)));

            foreach (var file in files)
            {
                var path = file.Path;

                _logger.LogTrace("Requesting: {0}", path);

                var sourceBlob = Container.GetBlobClient(path);

                _logger.LogTrace("Deleting: {0}", path);

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

                _logger.LogTrace("Blob successfully deleted!");
            }

            return response;
        }

        public async Task<BaseResponse> UploadFile(BlobUploadFile file)
        {
            var response = new BaseResponse();

            _logger.LogTrace("Uploading file to blob storage. Path: {0}", string.Join(", ", file.Path));

            var path = file.Path;

            _logger.LogTrace("Uploading: {0}", path);

            using(var mem = new MemoryStream(file.Content))
            {
                var uploadResponse = await Container.UploadBlobAsync(path, mem);

                var _response = uploadResponse?.GetRawResponse();
                if (CheckError(_response, out string msg))
                {
                    var errorMsg = $"Could not upload: {path}. Error: {msg}";
                    response.RequestError = errorMsg;
                    _logger.LogError(errorMsg);
                }
            }

            _logger.LogTrace("Blob successfully uploaded!");

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

            _logger.LogTrace("Uploading files to blob storage. Paths: {0}", string.Join(", ", files.Select(x => x.Path)));

            foreach (var file in files)
            {
                var path = file.Path;

                _logger.LogTrace("Uploading: {0}", path);

                using (var mem = new MemoryStream(file.Content))
                {
                    var uploadResponse = await Container.UploadBlobAsync(path, mem);

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

                    _logger.LogTrace("Blob successfully uploaded!");
                }
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

            _logger.LogTrace("Getting files from folder '{0}' with regex: {1}", folderName, regexPattern ?? "NO REGEX FILTER USED");

            await foreach (var blob in Container.GetBlobsByHierarchyAsync(prefix: folderName))
            {
                if (blob.IsBlob)
                {

                    _logger.LogTrace("Checking blob for folder: {0}", blob.Blob.Name);

                    if (!blob.Blob.Name.Contains($"{folderName}/"))
                    {
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(regexPattern))
                    {
                        _logger.LogTrace("Checking blob for regex: {0}", blob.Blob.Name);

                        var matches = Regex.Matches(blob.Blob.Name, regexPattern, RegexOptions.IgnoreCase);
                        if (matches.Count == 0)
                        {
                            continue;
                        }
                        else
                        {
                            _logger.LogTrace("Matching blob found: {0}", blob.Blob.Name);

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
                        _logger.LogTrace("Blob found: {0}", blob.Blob.Name);

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
            _logger.LogTrace("Moving blob file '{blob_name}' to folder '{dst}' with name '{name}'", blob.Blob.Name, destinationFolder, destinationName);

            var sourceBlob = Container.GetBlobClient(blob.Blob.Name);
            var destinationBlob = Container.GetBlobClient($"{destinationFolder}/{destinationName}");

            _logger.LogTrace("Copying between folders...");

            var copyResponse = await destinationBlob.StartCopyFromUriAsync(sourceBlob.Uri);

            if (copyResponse.HasValue)
            {
                throw new Exception(copyResponse.GetRawResponse().ReasonPhrase);
            }

            _logger.LogTrace("Deleting from original folder...");

            var deleteResponse = await sourceBlob.DeleteAsync();

            if (CheckError(deleteResponse, out string errorMessage))
            {
                throw new Exception(errorMessage);
            }

            _logger.LogTrace("Blob successfully moved to other folder!");
        }
    }
}
