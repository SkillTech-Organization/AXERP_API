using AXERP.API.BlobHelper.Managers;
using AXERP.API.Domain;

namespace AXERP.API.LogHelper.Factories
{
    /// <summary>
    /// Factory for creating <see cref="BlobManager"/> instances.
    /// </summary>
    public class BlobManagerFactory
    {
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor for BlobManagerFactory.
        /// </summary>
        /// <param name="serviceProvider">For getting <see cref="AxerpLoggerFactory"/></param>
        public BlobManagerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public BlobManager Create(string connectionString, string storageName)
        {
            return new BlobManager
            (
                (_serviceProvider.GetService(typeof(AxerpLoggerFactory)) as AxerpLoggerFactory)!,
                connectionString,
                storageName
            );
        }

        public BlobManager Create()
        {
            var connectionString = EnvironmentHelper.TryGetParameter("BlobStorageConnectionString");
            var blobStorageName = EnvironmentHelper.TryGetParameter("BlobStorageName");

            return new BlobManager
            (
                (_serviceProvider.GetService(typeof(AxerpLoggerFactory)) as AxerpLoggerFactory)!,
                connectionString,
                blobStorageName
            );
        }
    }
}
