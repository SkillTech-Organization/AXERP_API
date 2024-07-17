namespace AXERP.API.Domain.ServiceContracts.Responses
{
    public class ImportGasTransactionResponse : BaseResponse
    {
        public int TotalDataRowsInSheet { get; set; }

        public int ImportedRows { get; set; }

        public int InvalidRows { get; set; }

        public int NewRowsInsertedIntoDatabase { get; set; }
    }
}
