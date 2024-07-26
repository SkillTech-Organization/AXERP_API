using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Responses
{
    [DataContract]
    public class ProcessBlobFilesResponse : BaseResponse
    {
        [DataMember]
        public List<string> Processed { get; set; }

        [DataMember]
        public List<string> FailedToProcess { get; set; }

        [DataMember]
        public List<string> Errors { get; set; }
    }
}
