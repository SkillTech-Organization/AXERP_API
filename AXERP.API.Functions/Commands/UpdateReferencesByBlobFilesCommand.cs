using AXERP.API.BlobHelper.Managers;
using AXERP.API.BlobHelper.ServiceContracts.Responses;
using AXERP.API.Business.Factories;
using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;
using Microsoft.Extensions.Logging;

namespace AXERP.API.Functions.Commands
{
    public class UpdateReferencesByBlobFilesCommand
    {
        private readonly ILogger<UpdateReferencesByBlobFilesCommand> _logger;
        private readonly UnitOfWorkFactory _uowFactory;

        public UpdateReferencesByBlobFilesCommand(
            ILogger<UpdateReferencesByBlobFilesCommand> logger,
            UnitOfWorkFactory uowFactory)
        {
            _logger = logger;
            _uowFactory = uowFactory;
        }

        public async Task<ProcessBlobFilesResponse> Execute(ProcessBlobFilesRequest request)
        {
            var containerHelper = new BlobManager(request.BlobStorageConnectionString, request.BlobStorageName);

            var getBlobFilesResponse = await containerHelper.GetFiles(request.BlobStorageImportFolder, request.BlobStorePdfFileRegexPattern);

            var response = await Process(request, getBlobFilesResponse, containerHelper);

            return response;
        }

        private async Task<ProcessBlobFilesResponse> Process(ProcessBlobFilesRequest request, GetBlobFilesResponse data, BlobManager containerHelper)
        {
            var response = new ProcessBlobFilesResponse
            {
                Processed = new List<string>(),
                Errors = new List<string>()
            };

            if (data.Data.Count == 0)
            {
                return response;
            }

            try
            {
                string regexKey = Environment.GetEnvironmentVariable("RegexReferenceKey");
                if (string.IsNullOrWhiteSpace(regexKey))
                {
                    throw new Exception("Missing environment variable: RegexReferenceKey");
                }

                using (var uow = _uowFactory.Create())
                {
                    try
                    {
                        var entities = uow.DocumentRepository.GetAll();
                        var processed = new List<string>();

                        uow.BeginTransaction();

                        foreach (var item in data.Data)
                        {
                            var blob_name = item.BlobItem.Blob.Name;

                            try
                            {
                                var referenceName = item.Matches[0].Groups[regexKey].Value.Trim();
                                var fileName = item.Matches[0].Value;

                                var alreadyProcessed = entities.FirstOrDefault(x => x.OriginalName?.Trim() == referenceName);
                                if (alreadyProcessed != null)
                                {
                                    response.Errors.Add($"Document '{fileName}' was already processed at {alreadyProcessed.ProcessedAt}.");
                                    continue;
                                }

                                var referenced = entities.FirstOrDefault(x => x.Name.Trim() == referenceName);
                                if (referenced == null)
                                {
                                    response.Errors.Add($"No document found in database with name: {referenceName}");
                                    continue;
                                }

                                referenced.OriginalName = referenced.Name;
                                referenced.Name = fileName;
                                referenced.ProcessedAt = DateTime.Now;

                                uow.DocumentRepository.Update(referenced);

                                await containerHelper.MoveFile(item.BlobItem, fileName, request.BlobStorageProcessedFolder);

                                processed.Add(blob_name);
                            }
                            catch (Exception ex)
                            {
                                var name = item.BlobItem.Blob.Name;
                                _logger.LogError(ex, "Error while processing blob file: {name}", name);
                                response.Errors.Add($"Error while processing blob file: {name}, error: " + ex.Message);

                                continue;
                            }
                        }

                        uow.CommitTransaction();

                        response.Processed = processed;
                    }
                    catch (Exception ex)
                    {
                        uow.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing blob files");
                response.HttpStatusCode = System.Net.HttpStatusCode.InternalServerError;
                response.RequestError = ex.Message;
            }

            return response;
        }
    }
}
