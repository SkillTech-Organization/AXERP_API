using Microsoft.AspNetCore.Mvc;
using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Responses.Transactions
{
    [DataContract]
    public class GasTransactionCsvResponse : BaseResponse
    {
        [DataMember]
        public FileStreamResult File { get; set; }
    }
}
