using AXERP.API.Domain.Models;
using System.Runtime.Serialization;

namespace AXERP.API.BlobHelper.ServiceContracts.Requests
{
    [DataContract]
    public class UploadBlobFileRequest
    {
        public BlobUploadFile BlobUploadFile { get; set; }
    }
}
