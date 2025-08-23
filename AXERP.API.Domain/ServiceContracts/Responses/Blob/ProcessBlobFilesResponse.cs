using System.Runtime.Serialization;
using AXERP.API.Domain.ServiceContracts.Responses.Base;

namespace AXERP.API.Domain.ServiceContracts.Responses
{
    [DataContract]
    public class ProcessBlobFilesResponse : BaseResponse
    {
        [DataMember]
        public List<string> Processed { get; set; }

        [DataMember]
        public List<string> Errors { get; set; }

        [DataMember]
        public List<string> Warnings { get; set; }
    }
}
