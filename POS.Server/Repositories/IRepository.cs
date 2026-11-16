using System.Linq.Expressions;

namespace POS.Server.Repositories;

/// <summary>
/// Contrato genérico base para el patrón Repositorio.
/// Desacopla la lógica de negocio de la tecnología de persistencia subyacente.
/// </summary>
/// <typeparam name="T">Tipo de entidad del dominio</typeparam>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync();
}
