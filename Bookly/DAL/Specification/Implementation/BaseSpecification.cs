using DAL.Specification.Interfaces;
using System.Linq.Expressions;

namespace DAL.Specification.Implementation
{
    public class BaseSpecification<T> : ISpecification<T> where T : class
    {
        public Expression<Func<T, bool>>? Criteria { get; protected set; }

        public List<Expression<Func<T, object>>> Includes { get; protected set; } = new();

        public Expression<Func<T, object>>? OrderBy { get; protected set; }

        public Expression<Func<T, object>>? OrderByDescending { get; protected set; }

        public Expression<Func<T, object>> Selector { get; protected set; } = x => x;


        protected void ApplyInclude(Expression<Func<T, object>> include)
            => Includes.Add(include);

        protected void ApplyOrderBy(Expression<Func<T, object>> orderBy)
            => OrderBy = orderBy;

        protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescending)
            => OrderByDescending = orderByDescending;

        protected void ApplySelector(Expression<Func<T, object>> selector)
            => Selector = selector;
    }
}
