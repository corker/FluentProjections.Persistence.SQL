using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Threading.Tasks;
using Dapper;

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

        public async Task<IEnumerable<TProjection>> ReadAsync<TProjection>(IEnumerable<FilterValue> values)
            where TProjection : class
        {
            var conditions = new ExpandoObject();
            foreach (var x in values) ((IDictionary<string, object>)conditions).Add(x.Property.Name, x.Value);
            return await _connection.GetListAsync<TProjection>(conditions, _transaction);
        }

        public async Task UpdateAsync<TProjection>(TProjection projection) where TProjection : class
        {
            await _connection.UpdateAsync(projection, _transaction);
        }

        public async Task InsertAsync<TProjection>(TProjection projection) where TProjection : class
        {
            await _connection.InsertAsync(projection, _transaction);
        }

        public async Task RemoveAsync<TProjection>(IEnumerable<FilterValue> values) where TProjection : class
        {
            var conditions = new ExpandoObject();
            foreach (var x in values) ((IDictionary<string, object>)conditions).Add(x.Property.Name, x.Value);
            await _connection.DeleteListAsync<TProjection>(conditions, _transaction);
        }

        public Task CommitAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    _transaction.Commit();
                }
                finally
                {
                    _transaction.Dispose();
                    _transaction = null;
                }
            });
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