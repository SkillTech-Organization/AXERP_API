﻿using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Requests
{
    [DataContract]
    public class DeleteTransactionRequest : BaseRequest
    {
        [DataMember]
        public List<string> TransactionIds { get; set; }
    }
}