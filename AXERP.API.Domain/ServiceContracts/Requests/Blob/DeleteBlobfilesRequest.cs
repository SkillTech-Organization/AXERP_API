using AXERP.API.Domain.Models;
using AXERP.API.Domain.ServiceContracts.Responses.General;
using System.Runtime.Serialization;

namespace AXERP.API.BlobHelper.ServiceContracts.Requests
{
    [DataContract]
    public class DeleteBlobfilesRequest : BaseItemsRequest<BlobFile>
    {
    }
}
