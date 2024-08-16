using System.Runtime.Serialization;

namespace AXERP.API.Domain.Models
{
    [DataContract]
    public class BlobUploadFile : BlobFile
    {
        [DataMember]
        public Stream Content { get; set; }
    }
}
