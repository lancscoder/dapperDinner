using System;
using System.Linq;
using System.Linq.Expressions;

namespace DapperDinner.Models
{
    public interface IRepository<T>
    {
        IQueryable<T> All { get; }
        IQueryable<T> AllIncluding(params Expression<Func<T, object>>[] includeProperties);
        T Find(int id);
        void InsertOrUpdate(T dinner);
        void Delete(int id);
        void Save();
    }
}