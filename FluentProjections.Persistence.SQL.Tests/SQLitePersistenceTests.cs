using System.Data.SQLite;
using System.Linq;
using Dapper;
using DapperExtensions;
using DapperExtensions.Mapper;
using DapperExtensions.Sql;
using NUnit.Framework;

namespace FluentProjections.Persistence.SQL.Tests
{
    [TestFixture]
    public class SQLitePersistenceTests
    {
        private class TestAddNewMessage
        {
            public int Id { get; set; }
        }

        private class TestUpdateMessage
        {
            public int Id { get; set; }
            public int Value { get; set; }
        }

        public class TestProjection
        {
            public int Id { get; set; }
            public int Value { get; set; }
        }

        public class TestProjectionMapper : ClassMapper<TestProjection>
        {
            public TestProjectionMapper()
            {
                Map(x => x.Id).Key(KeyType.Assigned);
                Map(x => x.Value);
            }
        }

        private class TestHandler : MessageHandler<TestProjection>
        {
            public TestHandler(ICreateProjectionProviders providers) : base(providers)
            {
            }

            public void Handle(TestAddNewMessage message)
            {
                Handle(message, x => x
                    .AddNew()
                    .Map(m => m.Id));
            }

            public void Handle(TestUpdateMessage message)
            {
                Handle(message, x => x
                    .Update()
                    .WhenEqual(m => m.Id)
                    .Map(m => m.Value));
            }

            public void Handle(TestRemoveMessage message)
            {
                Handle(message, x => x
                    .Remove()
                    .WhenEqual(m => m.Id));
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            DapperExtensions.DapperExtensions.SqlDialect = new SqliteDialect();
        }

        private static SQLiteConnection CreateConnection()
        {
            var connection = new SQLiteConnection("FullUri=file::memory:?cache=shared");
            connection.Open();
            connection.Execute("CREATE TABLE TestProjection(Id INTEGER PRIMARY KEY, Value INTEGER)");
            return connection;
        }

        private class TestRemoveMessage
        {
            public int Id { get; set; }
        }

        private static SqlPersistenceFactory CreateFactory()
        {
            return new SqlPersistenceFactory(() => new SQLiteConnection("FullUri=file::memory:?cache=shared"));
        }

        [Test]
        public void When_add_new_message_should_add_a_new_record()
        {
            using (var connection = CreateConnection())
            {
                // Arrange
                var factory = CreateFactory();
                var target = new TestHandler(factory);

                // Act
                target.Handle(new TestAddNewMessage {Id = 777});

                // Assert
                var result = connection.GetList<TestProjection>().FirstOrDefault(x => x.Id == 777);
                Assert.IsNotNull(result);
            }
        }

        [Test]
        public void When_remove_message_should_remove_the_record()
        {
            using (var connection = CreateConnection())
            {
                // Arrange
                var factory = CreateFactory();
                var target = new TestHandler(factory);

                // Act
                target.Handle(new TestAddNewMessage {Id = 7777});
                target.Handle(new TestRemoveMessage {Id = 7777});

                // Assert
                var result = connection.GetList<TestProjection>().FirstOrDefault(x => x.Id == 7777);
                Assert.IsNull(result);
            }
        }

        [Test]
        public void When_update_message_should_update_the_record()
        {
            using (var connection = CreateConnection())
            {
                // Arrange
                var factory = CreateFactory();
                var target = new TestHandler(factory);

                // Act
                target.Handle(new TestAddNewMessage {Id = 7777});
                target.Handle(new TestUpdateMessage {Id = 7777, Value = 111});

                // Assert
                var result = connection.GetList<TestProjection>().First(x => x.Id == 7777);
                Assert.AreEqual(111, result.Value);
            }
        }
    }
}