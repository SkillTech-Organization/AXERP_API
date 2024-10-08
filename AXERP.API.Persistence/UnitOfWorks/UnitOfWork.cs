using AXERP.API.Domain.Interfaces.Repositories;
using AXERP.API.Domain.Interfaces.UnitOfWork;
using AXERP.API.Domain.Entities;
using AXERP.API.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using AXERP.API.Domain;

namespace AXERP.API.Persistence.UnitOfWorks
{
    public class UnitOfWork : IUnitOfWork
    {
        public SqlConnection Connection { get; private set; }

        public SqlTransaction Transaction { get; private set; }

        public UnitOfWork(string connectionStringKey)
        {
            Connection = new SqlConnection(EnvironmentHelper.TryGetParameter(connectionStringKey));
            Connection.Open();
        }

        private IGenericRepository _genericRepository;
        public IGenericRepository GenericRepository
        {
            get
            {
                if (_genericRepository == null)
                {
                    return new GenericRepository(this);
                }
                return _genericRepository;
            }
        }

        private IRepository<Document, int> _documentRepository;
        public IRepository<Document, int> DocumentRepository
        {
            get
            {
                if (_documentRepository == null)
                {
                    return new GenericEntityRepository<Document, int>(this);
                }
                return _documentRepository;
            }
        }

        private IRepository<CustomerToDelivery, int> _customerToDeliveryRepository;
        public IRepository<CustomerToDelivery, int> CustomerToDeliveryRepository
        {
            get
            {
                if (_customerToDeliveryRepository == null)
                {
                    return new GenericEntityRepository<CustomerToDelivery, int>(this);
                }
                return _customerToDeliveryRepository;
            }
        }

        private ITwoPartKeyRepository<Transaction, int, string> _transactionRepository;
        public ITwoPartKeyRepository<Transaction, int, string> TransactionRepository
        {
            get
            {
                if (_transactionRepository == null)
                {
                    return new GenericTwoPartKeyEntityRepository<Transaction, int, string>(this);
                }
                return _transactionRepository;
            }
        }

        private ITwoPartKeyRepository<Delivery, int, string> _deliveryRepository;
        public ITwoPartKeyRepository<Delivery, int, string> DeliveryRepository
        {
            get
            {
                if (_deliveryRepository == null)
                {
                    return new GenericTwoPartKeyEntityRepository<Delivery, int, string>(this);
                }
                return _deliveryRepository;
            }
        }

        private IRepository<Interface, int> _interfaceRepository;
        public IRepository<Interface, int> InterfaceRepository
        {
            get
            {
                if (_interfaceRepository == null)
                {
                    return new GenericEntityRepository<Interface, int>(this);
                }
                return _interfaceRepository;
            }
        }

        private IRepository<Entity, int> _entityRepository;
        public IRepository<Entity, int> EntityRepository
        {
            get
            {
                if (_entityRepository == null)
                {
                    return new GenericEntityRepository<Entity, int>(this);
                }
                return _entityRepository;
            }
        }

        private IRepository<TruckCompany, int> _truckCompanyRepository;
        public IRepository<TruckCompany, int> TruckCompanyRepository
        {
            get
            {
                if (_truckCompanyRepository == null)
                {
                    return new GenericEntityRepository<TruckCompany, int>(this);
                }
                return _truckCompanyRepository;
            }
        }

        private IRepository<TruckCompanyToDelivery, int> _truckCompanyToDeliveryRepository;
        public IRepository<TruckCompanyToDelivery, int> TruckCompanyToDeliveryRepository
        {
            get
            {
                if (_truckCompanyToDeliveryRepository == null)
                {
                    return new GenericEntityRepository<TruckCompanyToDelivery, int>(this);
                }
                return _truckCompanyToDeliveryRepository;
            }
        }

        private IRepository<TransactionStatus, int> _transactionStatusRepository;
        public IRepository<TransactionStatus, int> TransactionStatusRepository
        {
            get
            {
                if (_transactionStatusRepository == null)
                {
                    return new GenericEntityRepository<TransactionStatus, int>(this);
                }
                return _transactionStatusRepository;
            }
        }

        /// <summary>
        /// Begins a database transaction.
        /// This transaction won't affect the repositories used before opening it!
        /// </summary>
        public void BeginTransaction()
        {
            Transaction = Connection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            Transaction.Commit();
        }

        public void Save(string savePoint)
        {
            Transaction.Save(savePoint);
        }

        public void Rollback()
        {
            Transaction.Rollback();
        }

        public void Dispose()
        {
            //_transaction.Dispose();
            Connection.Dispose();
        }
    }
}
