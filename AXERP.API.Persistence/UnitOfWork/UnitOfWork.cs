using AXERP.API.Business.Interfaces.Repositories;
using AXERP.API.Business.Interfaces.UnitOfWork;
using AXERP.API.Domain.Entities;
using AXERP.API.Persistence.Repositories;
using Microsoft.Data.SqlClient;

namespace AXERP.API.Persistence.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        public SqlConnection Connection { get; private set; }

        public SqlTransaction Transaction { get; private set; }

        public UnitOfWork(string connectionStringKey)
        {
            Connection = new SqlConnection(Environment.GetEnvironmentVariable(connectionStringKey));
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

        private IRepository<Customer, int> _customerRepository;
        public IRepository<Customer, int> CustomerRepository
        {
            get
            {
                if (_customerRepository == null)
                {
                    return new GenericEntityRepository<Customer, int>(this);
                }
                return _customerRepository;
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

        private IRepository<Delivery, string> _deliveryRepository;
        public IRepository<Delivery, string> DeliveryRepository
        {
            get
            {
                if (_deliveryRepository == null)
                {
                    return new GenericEntityRepository<Delivery, string>(this);
                }
                return _deliveryRepository;
            }
        }

        private IRepository<Location, int> _locationRepository;
        public IRepository<Location, int> LocationRepository
        {
            get
            {
                if (_locationRepository == null)
                {
                    return new GenericEntityRepository<Location, int>(this);
                }
                return _locationRepository;
            }
        }

        private IRepository<Transporter, int> _transporterRepository;
        public IRepository<Transporter, int> TransporterRepository
        {
            get
            {
                if (_transporterRepository == null)
                {
                    return new GenericEntityRepository<Transporter, int>(this);
                }
                return _transporterRepository;
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
