using System.Net;

namespace AXERP.API.Domain.ServiceContracts.Responses.Base
{
    public class BaseResponse
    {
        public HttpStatusCode HttpStatusCode { get; set; } = HttpStatusCode.OK;

        public bool IsSuccess => HttpStatusCode == HttpStatusCode.OK;

        public string? RequestError { get; set; }
    }
}
