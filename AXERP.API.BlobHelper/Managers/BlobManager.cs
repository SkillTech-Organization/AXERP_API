using AXERP.API.BlobHelper.ServiceContracts.Responses;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.RegularExpressions;
using AXERP.API.Domain;

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

        public async Task<GetBlobFilesResponse> GetFiles(string? folderName = null, string? regexPattern = null)
        {
            var response = new GetBlobFilesResponse
            {
                Data = new List<GetBlobFilesItem>()
            };

            var result = new List<GetBlobFilesItem>();

            _logger.LogInformation("Getting files from folder '{folder}' with regex: {regex}", folderName, regexPattern ?? "NO REGEX FILTER USED");

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

            if (deleteResponse.IsError)
            {
                throw new Exception(deleteResponse.ReasonPhrase);
            }

            _logger.LogInformation("Blob successfully moved to other folder!");
        }
    }
}
