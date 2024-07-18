using AXERP.API.Domain.ServiceContracts.Responses;

namespace AXERP.API.Persistence.ServiceContracts.Responses
{
    public class GenericPagedQueryResponse<RowType> : BaseResponse
    {
        public IEnumerable<RowType> Data { get; set; }

        public int PageSize { get; set; }

        public int PageIndex { get; set; }

        public int TotalCount { get; set; }

        public int DataCount => Data?.Count() ?? 0;
    }
}
