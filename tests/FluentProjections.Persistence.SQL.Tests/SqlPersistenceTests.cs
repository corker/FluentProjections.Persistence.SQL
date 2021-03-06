﻿using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Xunit;

namespace FluentProjections.Persistence.SQL.Tests
{
    public class SqlPersistenceTests : IDisposable
    {
        public SqlPersistenceTests()
        {
            SimpleCRUD.SetDialect(SimpleCRUD.Dialect.SQLServer);
            _localdb = new LocalDb();
            using (var connection = _localdb.OpenConnection())
            {
                connection.Execute("CREATE TABLE TestProjection(Id INTEGER PRIMARY KEY, Value INTEGER)");
            }
        }

        public void Dispose()
        {
            _localdb.Dispose();
            _localdb = null;
        }

        private LocalDb _localdb;

        private SqlPersistenceFactory CreateFactory()
        {
            return new SqlPersistenceFactory(CreateConnectionImpl);
        }

        private IDbConnection CreateConnectionImpl()
        {
            return _localdb.OpenConnection();
        }

        private class TestAddNewMessage
        {
            public int Id { get; set; }
        }

        private class TestUpdateMessage
        {
            public int Id { get; set; }
            public int Value { get; set; }
        }

        private class TestProjection
        {
            [Key] [Required] public int Id { get; set; }

            public int Value { get; set; }
        }

        private class TestHandler : MessageHandler<TestProjection>
        {
            public TestHandler(ICreateProjectionProviders providers) : base(providers)
            {
            }

            public async Task HandleAsync(TestAddNewMessage message)
            {
                await HandleAsync(message, x => x
                    .AddNew()
                    .Map(m => m.Id));
            }

            public async Task HandleAsync(TestUpdateMessage message)
            {
                await HandleAsync(message, x => x
                    .Update()
                    .WhenEqual(m => m.Id)
                    .Map(m => m.Value));
            }

            public async Task HandleAsync(TestRemoveMessage message)
            {
                await HandleAsync(message, x => x
                    .Remove()
                    .WhenEqual(m => m.Id));
            }
        }

        private class TestRemoveMessage
        {
            public int Id { get; set; }
        }

        [Fact]
        public async Task When_add_new_message_should_add_a_new_record()
        {
            using (var connection = _localdb.OpenConnection())
            {
                // Arrange
                var factory = CreateFactory();
                var target = new TestHandler(factory);

                // Act
                await target.HandleAsync(new TestAddNewMessage {Id = 777});

                // Assert
                var result = connection.GetList<TestProjection>().FirstOrDefault(x => x.Id == 777);
                Assert.NotNull(result);
            }
        }

        [Fact]
        public async Task When_remove_message_should_remove_the_record()
        {
            using (var connection = _localdb.OpenConnection())
            {
                // Arrange
                var factory = CreateFactory();
                var target = new TestHandler(factory);

                // Act
                await target.HandleAsync(new TestAddNewMessage {Id = 7777});
                await target.HandleAsync(new TestRemoveMessage {Id = 7777});

                // Assert
                var result = connection.GetList<TestProjection>().FirstOrDefault(x => x.Id == 7777);
                Assert.Null(result);
            }
        }

        [Fact]
        public async Task When_update_message_should_update_the_record()
        {
            using (var connection = _localdb.OpenConnection())
            {
                // Arrange
                var factory = CreateFactory();
                var target = new TestHandler(factory);

                // Act
                await target.HandleAsync(new TestAddNewMessage {Id = 7777});
                await target.HandleAsync(new TestUpdateMessage {Id = 7777, Value = 111});

                // Assert
                var result = connection.GetList<TestProjection>().First(x => x.Id == 7777);
                Assert.Equal(111, result.Value);
            }
        }
    }
}