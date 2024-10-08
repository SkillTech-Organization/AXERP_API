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

        int Update(IEnumerable<RowType> entities, List<string>? columnFilter = null);

        int Delete(IEnumerable<RowType> entities);

        int Delete(Dictionary<string, object?> where);

        int Delete<TypeA, TypeB>((string, string) column, IEnumerable<(TypeA, TypeB)> values);

        bool Delete(RowType entity);

        bool Delete(KeyType id);

        int Delete(IEnumerable<KeyType> ids);

        int Delete(string column, object? value);

        int Delete(string column, IEnumerable<object?> values);

        List<RowType> Where(string column, object? value);
    }
}
