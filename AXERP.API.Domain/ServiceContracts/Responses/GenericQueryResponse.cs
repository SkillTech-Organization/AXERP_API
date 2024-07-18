namespace AXERP.API.Domain.ServiceContracts.Responses
{
    public class GenericQueryResponse<RowType> : BaseResponse
    {
        public IEnumerable<RowType> Data { get; set; }

        public int PageSize { get; set; }

        public int PageIndex { get; set; }

        public int TotalCount { get; set; }
    }
}
