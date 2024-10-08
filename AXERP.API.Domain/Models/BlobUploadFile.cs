using System.Runtime.Serialization;

namespace AXERP.API.Domain.Models
{
    [DataContract]
    public class BlobUploadFile : BlobFile
    {
        [DataMember]
        public byte[] Content { get; set; }
    }
}
