using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Requests.Transactions
{
    [DataContract]
    public class GetPagedGasTransactionsQueryRequest : PagedQueryRequest
    {
        [DataMember]
        public DateTime? FromDate { get; set; }

        [DataMember]
        public DateTime? ToDate { get; set; }

        [DataMember]
        public string Customer { get; set; }
    }
}
