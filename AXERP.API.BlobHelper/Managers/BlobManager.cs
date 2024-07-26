using AXERP.API.BlobHelper.ServiceContracts.Responses;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.RegularExpressions;

namespace AXERP.API.BlobHelper.Managers
{
    public class BlobManager
    {
        private readonly string ConnectionString = string.Empty;
        private readonly string CurrentStorage = string.Empty;

        private readonly BlobContainerClient Container;

        public BlobManager(string connectionString, string storageName)
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

            await foreach (var blob in Container.GetBlobsByHierarchyAsync(prefix: folderName))
            {
                if (blob.IsBlob)
                {
                    if (!blob.Blob.Name.Contains($"{folderName}/"))
                    {
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(regexPattern))
                    {
                        var matches = Regex.Matches(blob.Blob.Name, regexPattern, RegexOptions.IgnoreCase);
                        if (matches.Count == 0)
                        {
                            continue;
                        }
                        else
                        {
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
            var sourceBlob = Container.GetBlobClient(blob.Blob.Name);
            var destinationBlob = Container.GetBlobClient($"{destinationFolder}/{destinationName}");

            var copyResponse = await destinationBlob.StartCopyFromUriAsync(sourceBlob.Uri);

            if (copyResponse.HasValue)
            {
                throw new Exception(copyResponse.GetRawResponse().ReasonPhrase);
            }

            var deleteResponse = await sourceBlob.DeleteAsync();

            if (deleteResponse.IsError)
            {
                throw new Exception(deleteResponse.ReasonPhrase);
            }
        }
    }
}
