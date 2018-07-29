using System;
using System.Data;

namespace FluentProjections.Persistence.SQL
{
    public class SqlPersistenceFactory : ICreateProjectionProviders
    {
        private readonly Func<IDbConnection> _connections;

        public SqlPersistenceFactory(Func<IDbConnection> connections)
        {
            _connections = connections;
        }

        public IProvideProjections Create()
        {
            var connection = _connections();
            return new SqlPersistence(connection);
        }
    }
}