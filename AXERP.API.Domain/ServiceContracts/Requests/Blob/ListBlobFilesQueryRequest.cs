using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Requests
{
    [DataContract]
    public class ListBlobFilesQueryRequest
    {
        [DataMember]
        public string FolderName { get; set; }
    }
}
