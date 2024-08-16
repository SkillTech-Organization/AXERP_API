using AXERP.API.Domain.Models;
using AXERP.API.Domain.ServiceContracts.Responses;
using System.Runtime.Serialization;

namespace AXERP.API.BlobHelper.ServiceContracts.Requests
{
    [DataContract]
    public class DeleteBlobfilesResponse : BaseResponse
    {
        [DataMember]
        public List<BlobFile> Deleted { get; set; }

        [DataMember]
        public List<BlobFile> NotDeleted { get; set; }

        [DataMember]
        public List<string> Errors { get; set; }
    }
}
