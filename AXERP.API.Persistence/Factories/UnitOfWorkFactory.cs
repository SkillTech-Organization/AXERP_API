using AXERP.API.Domain.Interfaces.UnitOfWork;
using AXERP.API.Persistence.UnitOfWorks;

namespace AXERP.API.Persistence.Factories
{
    public class UnitOfWorkFactory
    {
        private string _connStringKey;

        public UnitOfWorkFactory(string connectionStringsKey = "SqlConnectionString")
        {
            _connStringKey = connectionStringsKey;
        }

        public IUnitOfWork Create()
        {
            return new UnitOfWork(_connStringKey);
        }
    }
}
