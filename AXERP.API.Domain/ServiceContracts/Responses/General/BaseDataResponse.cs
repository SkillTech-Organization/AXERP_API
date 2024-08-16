using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Responses.General
{
    [DataContract]
    public class BaseDataResponse<T> : BaseResponse where T : class
    {
        [DataMember]
        public List<T> Data { get; set; }
    }
}
