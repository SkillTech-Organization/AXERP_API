using Microsoft.Data.SqlClient;

namespace AXERP.API.Business.Interfaces.UnitOfWork
{
    public interface IConnectionProvider
    {
        SqlConnection Connection { get; }

        SqlTransaction Transaction { get; }
    }
}
