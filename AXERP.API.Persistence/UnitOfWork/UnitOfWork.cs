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

        private IRepository<Entity> _entityRepository;
        public IRepository<Entity> EntityRepository
        {
            get
            {
                if (_entityRepository == null)
                {
                    return new GenericEntityRepository<Entity>(this);
                }
                return _entityRepository;
            }
        }

        private IRepository<Customer> _customerRepository;
        public IRepository<Customer> CustomerRepository
        {
            get
            {
                if (_customerRepository == null)
                {
                    return new GenericEntityRepository<Customer>(this);
                }
                return _customerRepository;
            }
        }

        private IRepository<CustomerToDelivery> _customerToDeliveryRepository;
        public IRepository<CustomerToDelivery> CustomerToDeliveryRepository
        {
            get
            {
                if (_customerToDeliveryRepository == null)
                {
                    return new GenericEntityRepository<CustomerToDelivery>(this);
                }
                return _customerToDeliveryRepository;
            }
        }

        private IRepository<Delivery> _deliveryRepository;
        public IRepository<Delivery> DeliveryRepository
        {
            get
            {
                if (_deliveryRepository == null)
                {
                    return new GenericEntityRepository<Delivery>(this);
                }
                return _deliveryRepository;
            }
        }

        private IRepository<Location> _locationRepository;
        public IRepository<Location> LocationRepository
        {
            get
            {
                if (_locationRepository == null)
                {
                    return new GenericEntityRepository<Location>(this);
                }
                return _locationRepository;
            }
        }

        private IRepository<Transporter> _transporterRepository;
        public IRepository<Transporter> TransporterRepository
        {
            get
            {
                if (_transporterRepository == null)
                {
                    return new GenericEntityRepository<Transporter>(this);
                }
                return _transporterRepository;
            }
        }

        private IRepository<TruckCompany> _truckCompanyRepository;
        public IRepository<TruckCompany> TruckCompanyRepository
        {
            get
            {
                if (_truckCompanyRepository == null)
                {
                    return new GenericEntityRepository<TruckCompany>(this);
                }
                return _truckCompanyRepository;
            }
        }

        private IRepository<TruckCompanyToDelivery> _truckCompanyToDeliveryRepository;
        public IRepository<TruckCompanyToDelivery> TruckCompanyToDeliveryRepository
        {
            get
            {
                if (_truckCompanyToDeliveryRepository == null)
                {
                    return new GenericEntityRepository<TruckCompanyToDelivery>(this);
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
