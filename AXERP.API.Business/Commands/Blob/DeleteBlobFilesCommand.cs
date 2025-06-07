using AXERP.API.BlobHelper.ServiceContracts.Requests;
using AXERP.API.Business.Services;
using AXERP.API.Domain;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.Models;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;
using Microsoft.Extensions.Configuration;

namespace AXERP.API.Business.Commands.Blob
{
    [ForSystem("Blob Storage", LogConstants.FUNCTION_BL_PROCESSING)]
    public sealed class DeleteBlobFilesCommand : BaseAuditedClass<DeleteBlobFilesCommand>
    {
        private readonly BlobManagerFactory _blobManagerFactory;
        private readonly UnitOfWorkFactory _uowFactory;
        private readonly IConfiguration _configuration;
        private readonly IBillOfLadingUpdater _billOfLadingUpdater;

        public DeleteBlobFilesCommand(
            AxerpLoggerFactory axerpLoggerFactory,
            BlobManagerFactory blobManagerFactory,
            UnitOfWorkFactory uowFactory,
            IConfiguration configuration,
            IBillOfLadingUpdater billOfLadingUpdater) : base(axerpLoggerFactory)
        {
            _blobManagerFactory = blobManagerFactory;
            _uowFactory = uowFactory;
            _configuration = configuration;
            _billOfLadingUpdater = billOfLadingUpdater;
        }

        public async Task<DeleteBlobfilesResponse> Execute(DeleteBlobFilesRequest request)
        {
            try
            {
                var affectedTransactions = DeleteDocumentAndTransactionReferences(request.Items);

                var containerHelper = _blobManagerFactory.Create();
                var response = await containerHelper.DeleteFiles(request.Items);

                if (affectedTransactions.Any())
                {
                    await _billOfLadingUpdater.UpdateAsync(affectedTransactions, CancellationToken.None);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                return new DeleteBlobfilesResponse
                {
                    Errors = new List<string>
                    {
                        ex.Message
                    }
                };
            }
        }

        public IEnumerable<Transaction> DeleteDocumentAndTransactionReferences(List<BlobFile> toDelete)
        {
            var filesToDelete = toDelete
                .Where(x => x.Folder == _configuration.GetValue<string>("BlobStorageProcessedFolder"))
                .ToList();

            if (!filesToDelete.Any())
            {
                return Enumerable.Empty<Transaction>();
            }

            using (var uow = _uowFactory.Create())
            {
                try
                {
                    uow.BeginTransaction();

                    var documents = new List<Document>();
                    var transactions = new List<Transaction>();

                    foreach (var file in filesToDelete)
                    {
                        var doc = uow.DocumentRepository
                            .Where(nameof(Document.FileName), file.FileName)
                            .SingleOrDefault();

                        if (doc != null)
                        {
                            var refTrans = uow.TransactionRepository
                                .Where(nameof(Transaction.BlFileID), doc.ID);

                            transactions.AddRange(refTrans);
                            documents.Add(doc);
                        }
                    }

                    if (transactions.Any())
                    {
                        foreach (var tr in transactions)
                        {
                            tr.BlFileID = null;
                            tr.BillOfLading = null;
                        }
                        uow.TransactionRepository.Update(transactions);
                    }

                    uow.Save("transactions-updated");

                    if (documents.Any())
                    {
                        uow.DocumentRepository.Delete(documents);
                    }

                    uow.CommitTransaction();

                    return transactions;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex);
                    uow.Rollback();
                    throw;
                }
            }
        }
    }
}
