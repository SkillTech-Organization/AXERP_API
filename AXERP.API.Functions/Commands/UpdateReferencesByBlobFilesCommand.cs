using AutoMapper;
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
        private readonly IMapper _mapper;

        public UpdateReferencesByBlobFilesCommand(
            ILogger<UpdateReferencesByBlobFilesCommand> logger,
            UnitOfWorkFactory uowFactory,
            IMapper mapper)
        {
            _logger = logger;
            _uowFactory = uowFactory;
            _mapper = mapper;
        }

        public async Task<ProcessBlobFilesResponse> Execute(ProcessBlobFilesRequest request)
        {
            var containerHelper = new BlobManager(request.BlobStorageConnectionString, request.BlobStorageName);

            var getBlobFilesResponse = await containerHelper.GetFiles(request.BlobStorageImportFolder, request.BlobStorePdfFileRegexPattern);

            var response = await Update(request, getBlobFilesResponse, containerHelper);

            return response;
        }

        private async Task<ProcessBlobFilesResponse> Update(ProcessBlobFilesRequest request, GetBlobFilesResponse data, BlobManager containerHelper)
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
                        var entities = uow.EntityRepository.GetAll();
                        var processed = new List<string>();

                        uow.BeginTransaction();

                        foreach (var item in data.Data)
                        {
                            var blob_name = item.BlobItem.Blob.Name;

                            try
                            {
                                var referenceName = item.Matches[0].Groups[regexKey].Value.Trim();
                                var fileName = item.Matches[0].Value;
                                var referenced = entities.FirstOrDefault(x => x.Name.Trim() == referenceName);

                                referenced.Name = fileName;

                                uow.EntityRepository.Update(referenced);

                                await containerHelper.MoveFile(item.BlobItem, fileName, request.BlobStorageProcessedFolder);

                                processed.Add(blob_name);
                            }
                            catch (Exception ex)
                            {
                                var name = item.BlobItem.Blob.Name;

                                _logger.LogError(ex, "Error while processing blob file: {name}", name);

                                response.Errors.Add(name);

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
