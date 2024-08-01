namespace AXERP.API.Business.Interfaces.Repositories
{
    public interface IRepository<RowType, KeyType> where RowType : class
    {
        IEnumerable<RowType> GetAll();

        RowType GetById(KeyType id);

        int Add(List<RowType> entities, bool insertId = false);

        RowType Add(object entity, bool insertId = false);

        RowType Add(RowType entity, bool insertId = false);

        bool Update(RowType entity);

        bool Update(List<RowType> entities);

        bool Delete(RowType entity);

        bool Delete(KeyType id);

        List<RowType> Where(string column, object? value);
    }
}
