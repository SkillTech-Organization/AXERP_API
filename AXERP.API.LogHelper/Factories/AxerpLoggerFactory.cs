using AXERP.API.LogHelper.Managers;
using Microsoft.Extensions.Logging;

namespace AXERP.API.LogHelper.Factories
{
    /// <summary>
    /// Factory for creating <see cref="AxerpLogger"/> instances.
    /// </summary>
    public class AxerpLoggerFactory
    {
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor for LoggerFactory.
        /// </summary>
        /// <param name="serviceProvider">For getting <see cref="ILogger"/></param>
        public AxerpLoggerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public AxerpLogger<T> Create<T>() where T : class
        {
            return new AxerpLogger<T>((_serviceProvider.GetService(typeof(ILogger<T>)) as ILogger<T>)!);
        }
    }
}
