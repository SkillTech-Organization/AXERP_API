using AXERP.API.LogHelper.Base;
using AXERP.API.LogHelper.Factories;

namespace AXERP.API.Functions.Base
{
    public class BaseFunctions<T> : BaseAuditedClass<T> where T : class
    {
        protected string UserName { get; set; } = "Unknown";

        public BaseFunctions(AxerpLoggerFactory axerpLoggerFactory) : base(axerpLoggerFactory)
        {
        }
    }
}
