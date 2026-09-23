using System;

namespace My.XXX.APIs.Common;

/// <summary>Versioned endpoints own their envelope; legacy automatic wrapping must not apply.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ExplicitApiContractAttribute : NonUnifyResult { }
