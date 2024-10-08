using Microsoft.Data.SqlClient;

namespace AXERP.API.Domain.Interfaces.UnitOfWork
{
    public interface IConnectionProvider
    {
        SqlConnection Connection { get; }

        SqlTransaction Transaction { get; }
    }
}
