using AXERP.API.Domain.ServiceContracts.Responses;

namespace AXERP.API.Domain.ServiceContracts.Responses
{
    public class ImportGasTransactionResponse : BaseResponse
    {
        public int TotalDataRowsInSheet { get; set; }

        public int ImportedRows => TotalDataRowsInSheet - InvalidRows;

        public int InvalidRows { get; set; }

        public int NewRowsInsertedIntoDatabase { get; set; }

        public List<string> ImportErrors { get; set; }
    }
}
