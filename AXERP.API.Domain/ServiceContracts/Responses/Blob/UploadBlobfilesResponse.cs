using AXERP.API.Domain.Models;
using AXERP.API.Domain.ServiceContracts.Responses;
using System.Runtime.Serialization;

namespace AXERP.API.BlobHelper.ServiceContracts.Requests
{
    [DataContract]
    public class UploadBlobfilesResponse : BaseResponse
    {
        [DataMember]
        public List<BlobFile> Uploaded { get; set; }

        [DataMember]
        public List<BlobFile> NotUploaded { get; set; }

        [DataMember]
        public List<string> Errors { get; set; }
    }
}
