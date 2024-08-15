using AXERP.API.LogHelper.Attributes;
using AXERP.API.LogHelper.Factories;
using AXERP.API.LogHelper.Managers;
using System.Reflection;

namespace AXERP.API.LogHelper.Base
{
    public class BaseAuditedClass<T> where T : class
    {
        protected readonly AxerpLogger<T> _logger;
        protected readonly AxerpLoggerFactory _axerpLoggerFactory;

        public long ProcessId
        {
            get { return _logger.ProcessId; }
            set { _logger.SetNewId(value); }
        }

        // TODO: get this from inside AxerpLogger
        protected string ForSystem => typeof(T).GetCustomAttribute<ForSystemAttribute>()?.SystemName ?? typeof(T).Name;
        protected string ForFunction => typeof(T).GetCustomAttribute<ForSystemAttribute>()?.DefaultFunctionName ?? "Unknown Function";

        public string UserName { get; set; } = "Unknown";

        public BaseAuditedClass(AxerpLoggerFactory axerpLoggerFactory)
        {
            _logger = axerpLoggerFactory.Create<T>();
            _axerpLoggerFactory = axerpLoggerFactory;
        }

        public void SetLoggerProcessData(string? userName = null, long? processId = null)
        {
            UserName = userName ?? UserName;
            ProcessId = processId ?? ProcessId;
            _logger.SetData(user: UserName, system: ForSystem, function: ForFunction);
        }
    }
}
