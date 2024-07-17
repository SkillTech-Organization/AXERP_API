namespace AXERP.API.Domain.ServiceContracts.Responses
{
    public class ImportGasTransactionResponse : BaseResponse
    {
        public int ImportedRows { get; set; }

        public int InvalidRows { get; set; }

        public int NewRows { get; set; }

        public int UpdatedRows { get; set; }
    }
}
