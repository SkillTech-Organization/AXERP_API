using AXERP.API.LogHelper.Factories;
using AXERP.API.LogHelper.Managers;
using System.ComponentModel;
using System.Reflection;

namespace AXERP.API.Business.Commands
{
    public class BaseCommand<T> where T : class
    {
        protected readonly AxerpLogger<T> _logger;

        public long ProcessId
        {
            get { return _logger.ProcessId; }
            set { _logger.SetNewId(value); }
        }

        protected string System => typeof(T).GetCustomAttribute<DescriptionAttribute>()!.Description;

        public string UserName { get; set; } = "Unknown";

        public BaseCommand(AxerpLoggerFactory axerpLoggerFactory)
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
