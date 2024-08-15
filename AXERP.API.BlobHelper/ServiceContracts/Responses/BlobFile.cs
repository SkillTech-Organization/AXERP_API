using System.Runtime.Serialization;

namespace AXERP.API.BlobHelper.ServiceContracts.Responses
{
    [DataContract]
    public class BlobFile
    {
        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public string Folder { get; set; }
    }
}
