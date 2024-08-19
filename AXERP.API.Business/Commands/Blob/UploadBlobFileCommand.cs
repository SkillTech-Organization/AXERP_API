using AXERP.API.BlobHelper.ServiceContracts.Requests;
using AXERP.API.Domain;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;

namespace AXERP.API.Business.Commands
{
    [ForSystem("Blob Storage", LogConstants.FUNCTION_BL_PROCESSING)]
    public class UploadBlobFileCommand : BaseAuditedClass<UploadBlobFileCommand>
    {
        protected readonly BlobManagerFactory _blobManagerFactory;

        public UploadBlobFileCommand(
            AxerpLoggerFactory axerpLoggerFactory, BlobManagerFactory blobManagerFactory) : base(axerpLoggerFactory)
        {
            _blobManagerFactory = blobManagerFactory;
        }

        public async Task<BaseResponse> Execute(UploadBlobFileRequest request)
        {
            var containerHelper = _blobManagerFactory.Create();
            var response = await containerHelper.UploadFile(request.BlobUploadFile);

            return response;
        }
    }
}
