using AXERP.API.Business.Interfaces.Repositories;
using AXERP.API.Domain.Entities;

namespace AXERP.API.Business.Interfaces.UnitOfWork
{
    public interface IUnitOfWork : IDisposable, IConnectionProvider
    {
        IGenericRepository GenericRepository { get; }

        IRepository<Entity> EntityRepository { get; }

        IRepository<Customer> CustomerRepository { get; }

        IRepository<CustomerToDelivery> CustomerToDeliveryRepository { get; }

        IRepository<Delivery> DeliveryRepository { get; }

        IRepository<Location> LocationRepository { get; }

        IRepository<Transporter> TransporterRepository { get; }

        IRepository<TruckCompany> TruckCompanyRepository { get; }

        IRepository<TruckCompanyToDelivery> TruckCompanyToDeliveryRepository { get; }

        void BeginTransaction();

        void CommitTransaction();

        void Save(string savePoint);

        void Rollback();
    }
}
