using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Requests.CRUD
{
    [DataContract]
    public class DeleteTransactionRequest
    {
        [DataMember]
        public List<string> TransactionIds { get; set; }
    }
}
