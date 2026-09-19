using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Persistence.Common;
using System;
using System.Data;
using System.Data.Common;

namespace My.XXX.UnitTests;

[TestClass]
public class AtomicWriteTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void AtomicOutcomeControlsCommit(bool success)
    {
        var connection = new RecordingConnection();
        using var db = Create(connection);
        Assert.AreEqual(success, AtomicWrite.Execute(db, () => success, value => value));
        Assert.AreEqual(success, connection.Transaction.Committed);
        Assert.AreEqual(!success, connection.Transaction.RolledBack);
        Assert.IsTrue(connection.Transaction.Disposed);
    }

    [TestMethod]
    public void UnexpectedExceptionRollsBackAndPropagates()
    {
        var connection = new RecordingConnection();
        using var db = Create(connection);
        var expected = new InvalidOperationException("database failure");
        var actual = Assert.Throws<InvalidOperationException>(() =>
            AtomicWrite.Execute<bool>(db, () => throw expected, value => value));
        Assert.AreSame(expected, actual);
        Assert.IsFalse(connection.Transaction.Committed);
        Assert.IsTrue(connection.Transaction.RolledBack);
        Assert.IsTrue(connection.Transaction.Disposed);
    }

    private static DataConnection Create(DbConnection connection) => new(new DataOptions().UseConnection(
        SqlServerTools.GetDataProvider(SqlServerVersion.v2016, SqlServerProvider.MicrosoftDataSqlClient), connection));

    private sealed class RecordingConnection : DbConnection
    {
        private ConnectionState _state;
        public RecordingTransaction Transaction;
        public override string ConnectionString { get; set; }
        public override string Database => "test";
        public override string DataSource => "test";
        public override string ServerVersion => "13.0";
        public override ConnectionState State => _state;
        public override void Open() => _state = ConnectionState.Open;
        public override void Close() => _state = ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
            Transaction = new RecordingTransaction(this, isolationLevel);
    }

    private sealed class RecordingTransaction : DbTransaction
    {
        private readonly DbConnection _connection;
        public bool Committed, RolledBack, Disposed;
        public RecordingTransaction(DbConnection connection, IsolationLevel isolation) { _connection = connection; IsolationLevel = isolation; }
        public override IsolationLevel IsolationLevel { get; }
        protected override DbConnection DbConnection => _connection;
        public override void Commit() => Committed = true;
        public override void Rollback() => RolledBack = true;
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
}
