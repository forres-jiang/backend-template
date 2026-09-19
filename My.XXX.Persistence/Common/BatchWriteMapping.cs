using LinqToDB.Data;
using My.XXX.Service.DTOs;

namespace My.XXX.Persistence.Common;

internal static class BatchWriteMapping
{
    public static BatchWriteSummary ToSummary(this BulkCopyRowsCopied value) => new()
    {
        Abort = value.Abort,
        RowsCopied = value.RowsCopied,
        StartTime = value.StartTime
    };
}
