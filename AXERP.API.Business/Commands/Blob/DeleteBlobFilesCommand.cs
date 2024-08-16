using AXERP.API.BlobHelper.ServiceContracts.Requests;
using AXERP.API.Domain;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;

namespace AXERP.API.Business.Commands
{
    [ForSystem("Blob Storage", LogConstants.FUNCTION_BL_PROCESSING)]
    public class DeleteBlobFilesCommand : BaseAuditedClass<DeleteBlobFilesCommand>
    {
        protected readonly BlobManagerFactory _blobManagerFactory;

        public DeleteBlobFilesCommand(
            AxerpLoggerFactory axerpLoggerFactory, BlobManagerFactory blobManagerFactory) : base(axerpLoggerFactory)
        {
            _blobManagerFactory = blobManagerFactory;
        }

        public async Task<DeleteBlobfilesResponse> Execute(DeleteBlobfilesRequest request)
        {
            var containerHelper = _blobManagerFactory.Create();
            var response = await containerHelper.DeleteFiles(request.Items);

            return response;
        }
    }
}
