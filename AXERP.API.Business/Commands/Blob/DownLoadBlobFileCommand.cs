using AXERP.API.Domain;
using AXERP.API.Domain.ServiceContracts.Requests.Blob;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;

namespace AXERP.API.Business.Commands
{
    [ForSystem("Blob Storage", LogConstants.FUNCTION_BL_PROCESSING)]
    public class DownLoadBlobFileCommand : BaseAuditedClass<DownLoadBlobFileCommand>
    {
        protected readonly BlobManagerFactory _blobManagerFactory;

        public DownLoadBlobFileCommand(
            AxerpLoggerFactory axerpLoggerFactory, BlobManagerFactory blobManagerFactory) : base(axerpLoggerFactory)
        {
            _blobManagerFactory = blobManagerFactory;
        }

        public async Task<DownloadBlobFileResponse> Execute(DownloadBlobFileRequest request)
        {
            var containerHelper = _blobManagerFactory.Create();
            var response = await containerHelper.DownloadFile(request.FilePath);

            return response;
        }
    }
}
