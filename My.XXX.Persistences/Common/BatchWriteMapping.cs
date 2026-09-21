using LinqToDB.Data;
using My.XXX.Contracts.DTOs;

namespace My.XXX.Persistences.Common;

internal static class BatchWriteMapping
{
    public static BatchWriteSummary ToSummary(this BulkCopyRowsCopied value) => new()
    {
        Abort = value.Abort,
        RowsCopied = value.RowsCopied,
        StartTime = value.StartTime
    };
}
