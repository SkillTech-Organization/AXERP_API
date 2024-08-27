using AXERP.API.Domain.Interfaces.Repositories;
using AXERP.API.Domain.Entities;

namespace AXERP.API.Domain.Interfaces.UnitOfWork
{
    public interface IUnitOfWork : IDisposable, IConnectionProvider
    {
        IGenericRepository GenericRepository { get; }

        IRepository<Document, int> DocumentRepository { get; }

        IRepository<CustomerToDelivery, int> CustomerToDeliveryRepository { get; }

        IRepository<Transaction, string> TransactionRepository { get; }

        IRepository<Interface, int> InterfaceRepository { get; }

        IRepository<Delivery, string> DeliveryRepository { get; }

        IRepository<Entity, int> EntityRepository { get; }

        IRepository<TruckCompany, int> TruckCompanyRepository { get; }

        IRepository<TruckCompanyToDelivery, int> TruckCompanyToDeliveryRepository { get; }

        IRepository<TransactionStatus, int> TransactionStatusRepository { get; }

        void BeginTransaction();

        void CommitTransaction();

        void Save(string savePoint);

        void Rollback();
    }
}
