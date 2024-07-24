using AXERP.API.Domain.ServiceContracts.Requests;
using AXERP.API.Domain.ServiceContracts.Responses;

namespace AXERP.API.Business.Interfaces.Repositories
{
    public interface IGenericRepository
    {
        GenericPagedQueryResponse<dynamic> PagedQuery<RowType>(PagedQueryRequest request);

        IEnumerable<RowType> GetAll<RowType>(string tableName);

        int DeleteAll(string tableName);

        int CountAll(string tableName);

        IEnumerable<IdType> GetAllIDs<IdType>(string tableName);

        IEnumerable<RowType> GetAll<RowType>();

        int DeleteAll<RowType>();

        int CountAll<RowType>();

        IEnumerable<IdType> GetAllIDs<RowType, IdType>();
    }
}