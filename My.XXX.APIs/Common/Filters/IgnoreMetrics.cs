using System;

namespace My.XXX.APIs.Common;

/// <summary>Skip ordinary request telemetry; failures are still logged by the exception middleware.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IgnoreMetrics : Attribute { }
