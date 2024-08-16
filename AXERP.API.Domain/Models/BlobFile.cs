using System.Runtime.Serialization;

namespace AXERP.API.Domain.Models
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
