using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Requests.Transactions
{
    [DataContract]
    public class GasTransactionCsvRequest
    {
        [DataMember]
        public DateTime? FromDate { get; set; }

        [DataMember]
        public DateTime? ToDate { get; set; }

        [DataMember]
        public string Order { get; set; } = "";

        [DataMember]
        public bool OrderDesc { get; set; } = false;

        [DataMember]
        public List<string> Columns { get; set; }
    }
}
