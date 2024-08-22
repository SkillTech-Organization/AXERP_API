using System.Runtime.Serialization;

namespace AXERP.API.Domain.ServiceContracts.Requests
{
    [DataContract]
    public class ListBlobFilesQueryRequest
    {
        [DataMember]
        public string FolderName { get; set; }

        [DataMember]
        public List<string> ExcludeFolders { get; set; }
    }
}
