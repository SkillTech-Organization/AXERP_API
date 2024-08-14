using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Factories;
using AXERP.API.LogHelper.Managers;
using System.Reflection;

namespace AXERP.API.Business.Base
{
    public class BaseAuditedClass<T> where T : class
    {
        protected readonly AxerpLogger<T> _logger;

        public long ProcessId
        {
            get { return _logger.ProcessId; }
            set { _logger.SetNewId(value); }
        }

        // TODO: get this from inside AxerpLogger
        protected string System => typeof(T).GetCustomAttribute<SystemAttribute>()?.SystemName ?? typeof(T).Name;

        public string UserName { get; set; } = "Unknown";

        public BaseAuditedClass(AxerpLoggerFactory axerpLoggerFactory)
        {
            _logger = axerpLoggerFactory.Create<T>();
        }

        public void SetupLogger(string? userName = null, long? processId = null)
        {
            UserName = userName ?? UserName;
            ProcessId = processId ?? ProcessId;
            _logger.Set(user: UserName, system: System);
        }
    }
}
