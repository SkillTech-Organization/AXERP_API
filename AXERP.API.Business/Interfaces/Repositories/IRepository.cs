namespace AXERP.API.Business.Interfaces.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<IReadOnlyList<T>> GetAllAsync();

        Task<T> GetByIdAsync(int id);

        Task<string> AddAsync(T entity);

        Task<string> UpdateAsync(T entity);

        Task<string> DeleteAsync(T entity);
    }
}
