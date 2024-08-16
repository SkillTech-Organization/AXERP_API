using AXERP.API.Domain;
using AXERP.API.Domain.Models;
using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;
using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;
using AXERP.API.Persistence.Utils;

namespace AXERP.API.Business.Queries
{
    [ForSystem("Blob Storage", LogConstants.FUNCTION_BL_PROCESSING)]
    public class ListBlobFilesQuery : BaseAuditedClass<ListBlobFilesQuery>
    {
        protected readonly BlobManagerFactory _blobManagerFactory;

        public ListBlobFilesQuery(
            AxerpLoggerFactory axerpLoggerFactory, BlobManagerFactory blobManagerFactory) : base(axerpLoggerFactory)
        {
            _blobManagerFactory = blobManagerFactory;
        }

        public async Task<GenericPagedQueryResponse<BlobFile>> Execute(ListBlobFilesQueryRequest? request = null)
        {
            var containerHelper = _blobManagerFactory.Create();

            var files = await containerHelper.ListFiles(request?.FolderName);

            var response = new GenericPagedQueryResponse<BlobFile>
            {
                Data = files,
                Columns = typeof(BlobFile).GetColumnDatas(new List<string> { nameof(BlobFile.FileName), nameof(BlobFile.Folder) }),
                PageIndex = 1,
                PageSize = files.Count(),
                TotalCount = files.Count()
            };

            return response;
        }
    }
}
