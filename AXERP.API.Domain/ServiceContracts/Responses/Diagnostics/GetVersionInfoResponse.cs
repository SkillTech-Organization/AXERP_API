using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Responses.Diagnostics
{
    [DataContract]
    public class GetVersionInfoResponse : BaseResponse
    {
        [DataMember]
        public Version NETRunTimeVersion { get; set; }

        [DataMember]
        public Version AppVersion { get; set; }
    }
}
