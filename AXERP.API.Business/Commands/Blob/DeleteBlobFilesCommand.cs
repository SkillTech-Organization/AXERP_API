using AXERP.API.BlobHelper.ServiceContracts.Requests;
using AXERP.API.Domain;
using AXERP.API.Domain.Entities;
using AXERP.API.Domain.Models;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Factories;

namespace AXERP.API.Business.Commands
{
    [ForSystem("Blob Storage", LogConstants.FUNCTION_BL_PROCESSING)]
    public class DeleteBlobFilesCommand : BaseAuditedClass<DeleteBlobFilesCommand>
    {
        protected readonly BlobManagerFactory _blobManagerFactory;
        protected readonly UnitOfWorkFactory _uowFactory;

        public DeleteBlobFilesCommand(
            AxerpLoggerFactory axerpLoggerFactory,
            BlobManagerFactory blobManagerFactory,
            UnitOfWorkFactory uowFactory) : base(axerpLoggerFactory)
        {
            _blobManagerFactory = blobManagerFactory;
            _uowFactory = uowFactory;
        }

        public async Task<DeleteBlobfilesResponse> Execute(DeleteBlobFilesRequest request)
        {
            try
            {
                var containerHelper = _blobManagerFactory.Create();

                DeleteFromDatabase(request.Items);

                var response = await containerHelper.DeleteFiles(request.Items);

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

        public void DeleteFromDatabase(List<BlobFile> toDelete)
        {
            var processed = toDelete
                .Where(x => x.Folder == EnvironmentHelper.TryGetParameter("BlobStorageProcessedFolder"))
                .ToList();

            if (!processed.Any())
            {
                return;
            }

            using (var uow = _uowFactory.Create())
            {
                try
                {
                    uow.BeginTransaction();

                    var documents = new List<Document>();
                    var transactions = new List<Transaction>();

                    foreach (var d in processed)
                    {
                        var _doc = uow.DocumentRepository
                            .Where(nameof(Document.FileName), d.FileName)
                            .SingleOrDefault();

                        if (_doc != null)
                        {
                            var _refTrans = uow.TransactionRepository
                                .Where(nameof(Transaction.BlFileID), _doc.ID);

                            transactions.AddRange(_refTrans);
                            documents.Add(_doc);
                        }
                    }

                    if (transactions.Any())
                    {
                        foreach (var tr in transactions)
                        {
                            tr.BlFileID = null;
                        }
                        uow.TransactionRepository.Update(transactions);
                    }

                    uow.Save("transactions-updated");

                    if (documents.Any())
                    {
                        uow.DocumentRepository.Delete(documents);
                    }

                    uow.CommitTransaction();
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex);
                    uow.Rollback();
                    throw;
                }
            }
        }
    }
}
