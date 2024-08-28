using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Requests.Blob
{
    [DataContract]
    public class DownloadBlobFileRequest
    {
        [DataMember]
        public string FilePath { get; set; }
    }
}
