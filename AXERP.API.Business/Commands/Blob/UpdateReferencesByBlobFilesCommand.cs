using AXERP.API.BlobHelper.Managers;
using AXERP.API.BlobHelper.ServiceContracts.Responses;
using AXERP.API.Business.Services;
using AXERP.API.Domain;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.GoogleHelper;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;

namespace AXERP.API.Business.Commands.Blob
{
    [ForSystem("SQL Server, Blob Storage", LogConstants.FUNCTION_BL_PROCESSING)]
    public sealed class UpdateReferencesByBlobFilesCommand : BaseAuditedClass<UpdateReferencesByBlobFilesCommand>
    {
        private readonly UnitOfWorkFactory _uowFactory;
        private readonly BlobManagerFactory _blobManagerFactory;
        private readonly GoogleSheetManagerFactory _sheetManagerFactory;
        private readonly IBillOfLadingUpdater _billOfLadingUpdater;

        private readonly List<Transaction> _billOfLadingUpdated = new();

        public UpdateReferencesByBlobFilesCommand(
            AxerpLoggerFactory axerpLoggerFactory,
            UnitOfWorkFactory uowFactory,
            BlobManagerFactory blobManagerFactory,
            GoogleSheetManagerFactory sheetManagerFactory,
            IBillOfLadingUpdater billOfLadingUpdater) : base(axerpLoggerFactory)
        {
            _uowFactory = uowFactory;
            _blobManagerFactory = blobManagerFactory;
            _sheetManagerFactory = sheetManagerFactory;
            _billOfLadingUpdater = billOfLadingUpdater;
        }

        public void LogStatistics(ProcessBlobFilesResponse result)
        {
            if (result.Errors.Count == 0 && result.Warnings.Count == 0 && result.Processed.Count == 0)
            {
                _logger.LogInformation("No new file to process!");
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

        public async Task<ProcessBlobFilesResponse> ExecuteAsync(ProcessBlobFilesRequest request)
        {
            var containerHelper = _blobManagerFactory.Create();

            var getBlobFilesResponse = await containerHelper.GetFiles(request.BlobStorageImportFolder, request.BlobStorePdfFileRegexPattern);

            var response = await ProcessAsync(request, getBlobFilesResponse, containerHelper);

            if (_billOfLadingUpdated.Count > 0)
            {
                await _billOfLadingUpdater.UpdateAsync(_billOfLadingUpdated, CancellationToken.None);
                _logger.LogInformation("Bill of Lading updated for {0} transactions: {1}", _billOfLadingUpdated.Count, string.Join(", ", _billOfLadingUpdated.Select(x => x.ID + x.IDSffx)));
            }
            else
            {
                _logger.LogInformation("No Bill of Lading updated.");
            }

            LogStatistics(response);

            return response;
        }

        private async Task<ProcessBlobFilesResponse> ProcessAsync(ProcessBlobFilesRequest request, GetBlobFilesResponse getBlobFilesResponse, BlobManager containerHelper)
        {
            var response = new ProcessBlobFilesResponse
            {
                Processed = new List<string>(),
                Errors = new List<string>(),
                Warnings = new List<string>()
            };

            if (getBlobFilesResponse.Data.Count == 0)
            {
                return response;
            }

            _logger.LogInformation("Processing blob files. Amount of processable files found: {0}", getBlobFilesResponse.Data.Count);

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

                        foreach (var item in getBlobFilesResponse.Data)
                        {
                            var blobName = item.BlobItem.Blob.Name;

                            _logger.LogInformation("Querying transactions without BL File.");

                            var transactions = uow.TransactionRepository.Where(nameof(Transaction.BlFileID), null);
                            if (transactions == null)
                            {
                                throw new Exception("Query transactions without BL File failed!");
                            }

                            _logger.LogInformation("Transactions without BL File: {0}", transactions.Count);

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
                                var matchingTransactions = transactions
                                    .Where(x => x.Reference == referenceName ||
                                                x.Reference2 == referenceName ||
                                                x.Reference3 == referenceName)
                                    .ToArray();

                                _logger.LogInformation("Matching transactions: {0}", matchingTransactions.Length);
                                foreach (var transaction in matchingTransactions)
                                {
                                    transaction.BlFileID = referenced.ID;
                                    if (transaction.BillOfLading is null)
                                    {
                                        transaction.BillOfLading = DateTime.UtcNow;
                                        _billOfLadingUpdated.Add(transaction);
                                    }
                                }
                                uow.TransactionRepository.Update(matchingTransactions, new List<string> { nameof(Transaction.BlFileID), nameof(Transaction.BillOfLading) });
                                _logger.LogInformation("Matching transactions updated.");

                                _logger.LogInformation("Updating Document.");
                                uow.DocumentRepository.Update(referenced);
                                _logger.LogInformation("Document updated.");

                                await containerHelper.MoveFile(item.BlobItem, fileName, request.BlobStorageProcessedFolder);

                                processed.Add(blobName);
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
                    catch (Exception)
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
