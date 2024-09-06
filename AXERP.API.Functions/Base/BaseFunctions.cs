using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;
using Microsoft.Azure.Functions.Worker.Http;

namespace AXERP.API.Functions.Base
{
    public class BaseFunctions<T> : BaseAuditedClass<T> where T : class
    {
        protected string UserName { get; set; } = "Unknown";

        public BaseFunctions(AxerpLoggerFactory axerpLoggerFactory) : base(axerpLoggerFactory)
        {
        }

        public void SetLoggerProcessData(HttpRequestData req, string? system = null, string? function = null, long? id = null)
        {
            var isUserNameProvided = req.Headers.TryGetValues("x-user-name", out IEnumerable<string>? vals);
            UserName = isUserNameProvided ? (vals?.FirstOrDefault() ?? UserName) : UserName;
            SetLoggerProcessData(UserName, system, function, id);
        }
    }
}
