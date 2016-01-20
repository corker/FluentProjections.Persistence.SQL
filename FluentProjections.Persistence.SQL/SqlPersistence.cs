using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DapperExtensions;

namespace FluentProjections.Persistence.SQL
{
    /// <summary>
    ///     An implementation for <see cref="IProvideProjections" /> based on Dapper.
    /// </summary>
    public class SqlPersistence : IProvideProjections, IUnitOfWork, IDisposable
    {
        private readonly IDbConnection _connection;
        private bool _disposed;
        private IDbTransaction _transaction;

        public SqlPersistence(IDbConnection connection)
        {
            _connection = connection;
            _connection.Open();
            _transaction = _connection.BeginTransaction();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
            _connection.Update(projection, _transaction);
        }

        public void Insert<TProjection>(TProjection projection) where TProjection : class
        {
            _connection.Insert(projection, _transaction);
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
            _connection.Delete<TProjection>(predicate, _transaction);
        }

        public void Commit()
        {
            try
            {
                _transaction.Commit();
            }
            finally
            {
                _transaction.Dispose();
            }
            _transaction = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _transaction?.Dispose();
                _connection.Close();
                _connection.Dispose();
            }

            _disposed = true;
        }
    }
}