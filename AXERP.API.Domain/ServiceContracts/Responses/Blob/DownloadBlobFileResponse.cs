using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Responses
{
    [DataContract]
    public class DownloadBlobFileResponse : BaseResponse
    {
        [DataMember]
        public List<string> Errors { get; set; }

        [DataMember]
        public byte[] FileContent { get; set; }

        [DataMember]
        public string FileName { get; set; }
    }
}
