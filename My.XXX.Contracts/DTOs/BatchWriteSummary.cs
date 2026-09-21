using System;

namespace My.XXX.Contracts.DTOs;

/// <summary>Provider-independent batch outcome retaining the existing response fields.</summary>
public sealed class BatchWriteSummary
{
    public bool Abort { get; set; }
    public long RowsCopied { get; set; }
    public DateTime StartTime { get; set; }
}
