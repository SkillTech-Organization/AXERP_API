namespace AXERP.API.Domain.Interfaces.Repositories
{
    public interface IRepository<RowType, KeyType> where RowType : class
    {
        IEnumerable<RowType> GetAll();

        RowType GetById(KeyType id);

        int Add(List<RowType> entities, bool insertId = false);

        RowType Add(object entity, bool insertId = false);

        RowType Add(RowType entity, bool insertId = false);

        bool Update(RowType entity, List<string>? columnFilter = null);

        bool Update(IEnumerable<RowType> entities, List<string>? columnFilter = null);

        bool Delete(IEnumerable<RowType> entities);

        bool Delete(RowType entity);

        bool Delete(KeyType id);

        bool Delete(IEnumerable<KeyType> ids);

        bool Delete(string column, object? value);

        bool Delete(string column, IEnumerable<object?> values);

        List<RowType> Where(string column, object? value);
    }
}
