using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DapperExtensions;

namespace FluentProjections.Persistence.SQL
{
    /// <summary>
    ///     An implementation for <see cref="IPersistProjections" /> based on Dapper.
    /// </summary>
    public class SqlPersistence : IPersistProjections
    {
        private readonly IDbConnection _connection;

        public SqlPersistence(IDbConnection connection)
        {
            _connection = connection;
        }

        public IEnumerable<TProjection> Read<TProjection>(IEnumerable<FilterValue> values)
            where TProjection : class
        {
            var predicate = new PredicateGroup
            {
                Operator = GroupOperator.And,
                Predicates = values
                    .Select(x => new FieldPredicate<TProjection>
                    {
                        PropertyName = x.Property.Name,
                        Operator = Operator.Eq,
                        Value = x.Value
                    })
                    .Cast<IPredicate>()
                    .ToList()
            };
            return _connection.GetList<TProjection>(predicate);
        }

        public void Update<TProjection>(TProjection projection) where TProjection : class
        {
            _connection.Update(projection);
        }

        public void Insert<TProjection>(TProjection projection) where TProjection : class
        {
            _connection.Insert(projection);
        }

        public void Remove<TProjection>(IEnumerable<FilterValue> values) where TProjection : class
        {
            var predicate = new PredicateGroup
            {
                Operator = GroupOperator.And,
                Predicates = values
                    .Select(x => new FieldPredicate<TProjection>
                    {
                        PropertyName = x.Property.Name,
                        Operator = Operator.Eq,
                        Value = x.Value
                    })
                    .Cast<IPredicate>()
                    .ToList()
            };
            _connection.Delete<TProjection>(predicate);
        }
    }
}