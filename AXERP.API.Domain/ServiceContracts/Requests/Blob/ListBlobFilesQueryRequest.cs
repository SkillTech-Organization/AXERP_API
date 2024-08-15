using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Requests
{
    [DataContract]
    public class ListBlobFilesQueryRequest
    {
        [DataMember]
        public string BlobStorageConnectionString { get; set; }

        [DataMember]
        public string BlobStorageName { get; set; }
    }
}
