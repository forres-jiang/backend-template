using LinqToDB.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace My.XXX.Persistence.Common;

/// <summary>Executes one repository write operation atomically; unexpected exceptions propagate.</summary>
internal static class AtomicWrite
{
    public static async Task<T> ExecuteAsync<T>(DataConnection connection, Func<Task<T>> action,
        Func<T, bool> succeeded, CancellationToken cancellationToken = default)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action();
            cancellationToken.ThrowIfCancellationRequested();
            if (succeeded(result)) await transaction.CommitAsync(cancellationToken);
            else await transaction.RollbackAsync(CancellationToken.None);
            return result;
        }
        catch
        {
            // Cleanup must still run after the HTTP request is cancelled.
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
    public static T Execute<T>(DataConnection connection, Func<T> action, Func<T, bool> succeeded)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            var result = action();
            if (succeeded(result)) transaction.Commit();
            else transaction.Rollback();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
