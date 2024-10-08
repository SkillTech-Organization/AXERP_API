using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Requests
{
    [DataContract]
    public class BaseRequest
    {
        [DataMember]
        public virtual string UserName { get; set; } = "Unknown";
    }
}
