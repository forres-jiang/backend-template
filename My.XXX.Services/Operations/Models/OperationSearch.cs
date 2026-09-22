using System;
namespace My.XXX.Services.Operations.Models;

public sealed record OperationSearch(string Controller, string Action, DateTime? Date, int Offset, int Limit);
