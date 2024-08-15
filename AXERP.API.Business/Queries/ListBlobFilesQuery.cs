using AXERP.API.BlobHelper.Managers;
using AXERP.API.BlobHelper.ServiceContracts.Responses;
using AXERP.API.Domain;
using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses.General;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;

namespace AXERP.API.Business.Queries
{
    [ForSystem("Blob Storage", LogConstants.FUNCTION_BL_PROCESSING)]
    public class ListBlobFilesQuery : BaseAuditedClass<ListBlobFilesQuery>
    {
        public ListBlobFilesQuery(
            AxerpLoggerFactory axerpLoggerFactory) : base(axerpLoggerFactory)
        {
        }

        public async Task<DataResponse<BlobFile>> Execute(ListBlobFilesQueryRequest request)
        {
            var containerHelper = new BlobManager(_axerpLoggerFactory, request.BlobStorageConnectionString, request.BlobStorageName);

            var files = await containerHelper.ListFiles();

            return new DataResponse<BlobFile>
            {
                Data = files
            };
        }
    }
}
