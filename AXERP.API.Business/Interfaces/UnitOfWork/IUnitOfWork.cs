using AXERP.API.Business.Interfaces.Repositories;
using AXERP.API.Domain.Entities;

namespace AXERP.API.Business.Interfaces.UnitOfWork
{
    public interface IUnitOfWork : IDisposable, IConnectionProvider
    {
        IGenericRepository GenericRepository { get; }

        IRepository<Entity, int> EntityRepository { get; }

        IRepository<Customer, int> CustomerRepository { get; }

        IRepository<CustomerToDelivery, int> CustomerToDeliveryRepository { get; }

        IRepository<Delivery, string> DeliveryRepository { get; }

        IRepository<Location, int> LocationRepository { get; }

        IRepository<Transporter, int> TransporterRepository { get; }

        IRepository<TruckCompany, int> TruckCompanyRepository { get; }

        IRepository<TruckCompanyToDelivery, int> TruckCompanyToDeliveryRepository { get; }

        void BeginTransaction();

        void CommitTransaction();

        void Save(string savePoint);

        void Rollback();
    }
}
