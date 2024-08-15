using AXERP.API.LogHelper.Factories;
using AXERP.API.LogHelper.Managers;

namespace AXERP.API.LogHelper.Base
{
    public class BaseAuditedClass<T> where T : class
    {
        protected readonly AxerpLogger<T> _logger;
        protected readonly AxerpLoggerFactory _axerpLoggerFactory;

        public BaseAuditedClass(AxerpLoggerFactory axerpLoggerFactory)
        {
            _logger = axerpLoggerFactory.Create<T>();
            _axerpLoggerFactory = axerpLoggerFactory;
        }

        public void SetLoggerProcessData(string? user = null, string? system = null, string? function = null, long? id = null)
        {
            _logger.SetLoggerProcessData(user, system, function, id);
        }
    }
}
