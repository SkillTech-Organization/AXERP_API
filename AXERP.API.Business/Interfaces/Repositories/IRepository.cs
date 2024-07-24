namespace AXERP.API.Business.Interfaces.Repositories
{
    public interface IRepository<RowType> where RowType : class
    {
        IEnumerable<RowType> GetAll();

        RowType GetById(int id);

        int Add(List<RowType> entities, bool insertId = false);

        RowType Add(object entity, bool insertId = false);

        RowType Add(RowType entity, bool insertId = false);

        bool Update(RowType entity);

        bool Delete(RowType entity);
    }
}
