#nullable enable
using My.XXX.Persistences.PersistentObjects;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Models;
using Riok.Mapperly.Abstractions;
using System.Collections.Generic;

namespace My.XXX.Persistences.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target,
    ThrowOnMappingNullMismatch = false, ThrowOnPropertyMappingNullMismatch = false)]
public partial class PersistenceMapper
{
    [MapperIgnoreTarget(nameof(Demo.DemoGUID))]
    [MapperIgnoreTarget(nameof(Demo.DemoBoolean))]
    [MapperIgnoreTarget(nameof(Demo.DemoDecimal))]
    public partial Demo? ToDemo(DemoModel? source);

    [MapperIgnoreTarget(nameof(DemoDetail.Id))]
    public partial DemoDetail? ToDemoDetail(DemoDetailModel? source);

    public partial List<DemoDetail> ToDemoDetails(IEnumerable<DemoDetailModel>? source);

    public partial MenuState? ToMenuState(Menus? source);
    public partial List<MenuState> ToMenuStates(IEnumerable<Menus>? source);
    public partial Menus? ToMenuEntity(MenuState? source);
    // Values arrive serialized at the boundary; do not serialize JSON a second time.
    public Operation? ToOperation(MetricsInfo? source)
    {
        if (source is null)
            return null;
        var result = MapOperationCore(source);
        result.Inputs = source.Inputs ?? "null";
        result.ReturnValue = source.ReturnValue ?? "null";
        return result;
    }

    [MapperIgnoreTarget(nameof(Operation.Inputs))]
    [MapperIgnoreTarget(nameof(Operation.ReturnValue))]
    private partial Operation MapOperationCore(MetricsInfo source);

    public partial OperationDto? ToOperationDto(Operation? source);

    public partial List<OperationDto> ToOperationDtos(IEnumerable<Operation>? source);
}
