using AXERP.API.Business.Interfaces.UnitOfWork;
using AXERP.API.Persistence.UnitOfWork;

namespace AXERP.API.Business.Factories
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
