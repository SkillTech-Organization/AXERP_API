﻿using AXERP.API.BlobHelper.Managers;
using AXERP.API.BlobHelper.ServiceContracts.Responses;
using AXERP.API.Domain;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;
using Transaction = AXERP.API.Domain.Entities.Transaction;

namespace AXERP.API.Business.Commands
{
    [ForSystem("SQL Server, Blob Storage", LogConstants.FUNCTION_BL_PROCESSING)]
    public class UpdateReferencesByBlobFilesCommand : BaseAuditedClass<UpdateReferencesByBlobFilesCommand>
    {
        private readonly UnitOfWorkFactory _uowFactory;
        protected readonly BlobManagerFactory _blobManagerFactory;

        public UpdateReferencesByBlobFilesCommand(
            AxerpLoggerFactory axerpLoggerFactory,
            UnitOfWorkFactory uowFactory,
            BlobManagerFactory blobManagerFactory) : base(axerpLoggerFactory)
        {
            _uowFactory = uowFactory;
            _blobManagerFactory = blobManagerFactory;
        }

        public void LogStatistics(ProcessBlobFilesResponse result)
        {
            if (result.Errors.Count == 0 && result.Warnings.Count == 0 && result.Processed.Count == 0)
            {
                _logger.LogInformation("There are no blob files to process.");
            }
            else if (result.Errors.Count == 0 && result.Warnings.Count > 0 && result.Processed.Count == 0)
            {
                _logger.LogInformation("There were no processable blob files.");
            }
            else if (result.Errors.Count == 0)
            {
                _logger.LogInformation("Success! Process BL files statistics: {0}", Newtonsoft.Json.JsonConvert.SerializeObject(result));
            }
            else if (result.Errors.Count > 0 && result.Processed.Count > 0)
            {
                _logger.LogWarning("Not all files could be processed! Process BL files statistics: {0}", Newtonsoft.Json.JsonConvert.SerializeObject(result));
            }
            else if (result.Errors.Count > 0 && result.Processed.Count == 0)
            {
                _logger.LogError("Error! No file could be processed! Process BL files statistics: {0}", Newtonsoft.Json.JsonConvert.SerializeObject(result));
            }
        }

        public async Task<ProcessBlobFilesResponse> Execute(ProcessBlobFilesRequest request)
        {
            var containerHelper = _blobManagerFactory.Create();

            var getBlobFilesResponse = await containerHelper.GetFiles(request.BlobStorageImportFolder, request.BlobStorePdfFileRegexPattern);

            var response = await Process(request, getBlobFilesResponse, containerHelper);

            LogStatistics(response);

            return response;
        }

        private async Task<ProcessBlobFilesResponse> Process(ProcessBlobFilesRequest request, GetBlobFilesResponse data, BlobManager containerHelper)
        {
            var response = new ProcessBlobFilesResponse
            {
                Processed = new List<string>(),
                Errors = new List<string>(),
                Warnings = new List<string>()
            };

            if (data.Data.Count == 0)
            {
                return response;
            }

            _logger.LogInformation("Processing blob files. Amount of processable files found: {0}", data.Data.Count);

            try
            {
                string regexKey = EnvironmentHelper.TryGetParameter("RegexReferenceKey");
                if (string.IsNullOrWhiteSpace(regexKey))
                {
                    throw new Exception("Missing environment variable: RegexReferenceKey");
                }

                using (var uow = _uowFactory.Create())
                {
                    try
                    {
                        var entities = uow.DocumentRepository
                            .GetAll()
                            .ToList();
                        var processed = new List<string>();

                        uow.BeginTransaction();

                        foreach (var item in data.Data)
                        {

                            var blob_name = item.BlobItem.Blob.Name;

                            _logger.LogInformation("Querying transactions without BL File.");

                            var transactrions = uow.TransactionRepository.Where(nameof(Transaction.BlFileID), null);
                            if (transactrions == null)
                            {
                                throw new Exception("Query transactions without BL File failed!");
                            }

                            _logger.LogInformation("Transactions without BL File: {0}", transactrions.Count());

                            try
                            {
                                var referenceName = item.Matches[0].Groups[regexKey].Value.Trim();
                                var fileName = item.Matches[0].Value;

                                _logger.LogInformation("Processing: {0}", fileName);

                                var referenced = entities.FirstOrDefault(x => x.Name?.Trim() == referenceName);
                                if (referenced != null && !string.IsNullOrWhiteSpace(referenced.FileName))
                                {
                                    var msg = $"Blob file '{fileName}' was already processed at {referenced.ProcessedAt}.";
                                    _logger.LogWarning(msg);
                                    response.Warnings.Add(msg);
                                    continue;
                                }

                                if (referenced == null)
                                {
                                    var msg = $"No Document record found with name: {referenceName}. New one will be inserted.";

                                    _logger.LogWarning(msg);

                                    referenced = uow.DocumentRepository.Add(new Document { Name = referenceName });
                                    uow.Save("referenced-" + referenceName);

                                    entities.Add(referenced);
                                }

                                referenced.FileName = fileName;
                                referenced.ProcessedAt = DateTime.Now;

                                // Updating transactions without a bl file
                                // Order of priority: Reference > Reference2 > Reference3
                                var matchingTransactions = transactrions
                                    .Where(x => x.Reference == referenceName ||
                                                x.Reference2 == referenceName ||
                                                x.Reference3 == referenceName);

                                _logger.LogInformation("Matching transactions: {0}", matchingTransactions.Count());
                                foreach (var transaction in matchingTransactions)
                                {
                                    transaction.BlFileID = referenced.ID;
                                }
                                uow.TransactionRepository.Update(matchingTransactions, new List<string> { nameof(Transaction.BlFileID) });
                                _logger.LogInformation("Matching transactions updated.");

                                _logger.LogInformation("Updating Document.");
                                uow.DocumentRepository.Update(referenced);
                                _logger.LogInformation("Document updated.");

                                await containerHelper.MoveFile(item.BlobItem, fileName, request.BlobStorageProcessedFolder);

                                processed.Add(blob_name);
                            }
                            catch (Exception ex)
                            {
                                var name = item.BlobItem.Blob.Name;
                                _logger.LogError(ex, "Error while processing blob file: {0}", name);
                                response.Errors.Add($"Error while processing blob file: {name}, error: " + ex.Message);

                                continue;
                            }
                        }

                        uow.CommitTransaction();

                        response.Processed = processed;

                        _logger.LogInformation("All blob files processed.");
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
