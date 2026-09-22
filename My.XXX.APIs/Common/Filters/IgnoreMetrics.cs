using System;

namespace My.XXX.APIs.Common;

/// <summary>跳过常规请求遥测；失败仍会由异常中间件记录日志。</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IgnoreMetrics : Attribute { }
