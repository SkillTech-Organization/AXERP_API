using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Responses.General
{
    [DataContract]
    public class BaseItemsRequest<T>
    {
        [DataMember]
        public List<T> Items { get; set; }
    }
}
