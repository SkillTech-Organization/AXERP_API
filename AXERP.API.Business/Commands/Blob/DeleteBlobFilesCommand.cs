using AXERP.API.BlobHelper.ServiceContracts.Requests;
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


        public DeleteBlobFilesCommand(
            AxerpLoggerFactory axerpLoggerFactory,
            BlobManagerFactory blobManagerFactory,
            UnitOfWorkFactory uowFactory,
            IConfiguration configuration) : base(axerpLoggerFactory)
        {
            _blobManagerFactory = blobManagerFactory;
            _uowFactory = uowFactory;
            _configuration = configuration;
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
            var filesToDelete = toDelete
                .Where(x => x.Folder == _configuration.GetValue<string>("BlobStorageProcessedFolder"))
                .ToList();

            if (!filesToDelete.Any())
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
