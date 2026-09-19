using LinqToDB.Data;
using System;

namespace My.XXX.Persistence.Common;

/// <summary>Executes one repository write operation atomically; unexpected exceptions propagate.</summary>
internal static class AtomicWrite
{
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
