using System;

namespace My.XXX.Contracts.DTOs;

/// <summary>与具体提供程序无关的批量写入结果，保留现有的响应字段。</summary>
public sealed class BatchWriteSummary
{
    public bool Abort { get; set; }
    public long RowsCopied { get; set; }
    public DateTime StartTime { get; set; }
}
