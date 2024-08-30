namespace AXERP.API.Domain.Interfaces.Repositories
{
    public interface ITwoPartKeyRepository<RowType, KeyTypeA, KeyTypeB> where RowType : class
    {
        IEnumerable<RowType> GetAll();

        RowType GetById(KeyTypeA idA, KeyTypeB idB);

        int Add(List<RowType> entities, bool insertId = false);

        RowType Add(object entity, bool insertId = false);

        RowType Add(RowType entity, bool insertId = false);

        bool Update(RowType entity, List<string>? columnFilter = null);

        int Update(IEnumerable<RowType> entities, List<string>? columnFilter = null);

        int Delete(IEnumerable<RowType> entities);

        bool Delete(RowType entity);

        bool Delete(KeyTypeA idA, KeyTypeB idB);

        int Delete(IEnumerable<(KeyTypeA, KeyTypeB)> ids);

        int Delete(string column, object? value);

        int Delete(string column, IEnumerable<object?> values);

        List<RowType> Where(string column, object? value);
    }
}
