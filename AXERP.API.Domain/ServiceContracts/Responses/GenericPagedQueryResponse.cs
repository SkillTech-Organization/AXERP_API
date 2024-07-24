using AXERP.API.Domain.Models;

namespace AXERP.API.Domain.ServiceContracts.Responses
{
    public class GenericPagedQueryResponse<RowType> : BaseResponse
    {
        public List<RowType> Data { get; set; }

        public List<ColumnData> Columns { get; set; }

        public int PageSize { get; set; }

        public int PageIndex { get; set; }

        public int TotalCount { get; set; }

        public int DataCount => Data?.Count() ?? 0;
    }
}
