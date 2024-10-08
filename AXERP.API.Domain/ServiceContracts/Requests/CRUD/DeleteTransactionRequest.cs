using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Requests
{
    [DataContract]
    public class DeleteTransactionRequest
    {
        [DataMember]
        public List<string> TransactionIds { get; set; }
    }
}
