using AXERP.API.Domain.ServiceContracts.Responses.Base;

namespace AXERP.API.Domain.ServiceContracts.Responses.Transactions
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
