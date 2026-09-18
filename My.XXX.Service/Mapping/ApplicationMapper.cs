#nullable enable
using Newtonsoft.Json;
using My.XXX.Data.PersistantObjects;
using My.XXX.Service.DTOs;
using Riok.Mapperly.Abstractions;
using System.Collections.Generic;

namespace My.XXX.Service.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target,
    ThrowOnMappingNullMismatch = false, ThrowOnPropertyMappingNullMismatch = false)]
public partial class ApplicationMapper
{
    [MapperIgnoreTarget(nameof(Demo.DemoGUID))]
    [MapperIgnoreTarget(nameof(Demo.DemoBoolean))]
    [MapperIgnoreTarget(nameof(Demo.DemoDecimal))]
    public partial Demo? ToDemo(DemoModel? source);

    [MapperIgnoreTarget(nameof(DemoDetail.Id))]
    public partial DemoDetail? ToDemoDetail(DemoDetailModel? source);

    public partial List<DemoDetail> ToDemoDetails(IEnumerable<DemoDetailModel>? source);

    [MapperIgnoreTarget(nameof(MailQueue.MAILSEQ))]
    [MapperIgnoreTarget(nameof(MailQueue.APPCODE))]
    [MapperIgnoreTarget(nameof(MailQueue.BCC))]
    [MapperIgnoreTarget(nameof(MailQueue.ORGANISATION))]
    [MapperIgnoreTarget(nameof(MailQueue.SUBMITBY))]
    [MapperIgnoreTarget(nameof(MailQueue.SUBMITDATE))]
    [MapperIgnoreTarget(nameof(MailQueue.POSTEDFLAG))]
    [MapperIgnoreTarget(nameof(MailQueue.IMMEDIATEFLAG))]
    [MapperIgnoreTarget(nameof(MailQueue.POSTDATE))]
    [MapperIgnoreTarget(nameof(MailQueue.ERRMSG))]
    [MapperIgnoreTarget(nameof(MailQueue.ENCODE))]
    [MapperIgnoreTarget(nameof(MailQueue.ReferID))]
    public partial MailQueue? ToMailQueue(Mail? source);

    [MapperIgnoreTarget(nameof(Menus.Id))]
    [MapperIgnoreTarget(nameof(Menus.DisplayNames))]
    [MapperIgnoreTarget(nameof(Menus.IsDeleted))]
    [MapperIgnoreTarget(nameof(Menus.CreatedBy))]
    [MapperIgnoreTarget(nameof(Menus.CreatedTime))]
    [MapperIgnoreTarget(nameof(Menus.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Menus.UpdatedTime))]
    public partial Menus? ToMenu(SaveMenu? source);

    public partial SaveMenu? ToSaveMenu(Menus? source);

    [MapperIgnoreTarget(nameof(MenuDto.Checked))]
    [MapperIgnoreTarget(nameof(MenuDto.Actions))]
    [MapperIgnoreTarget(nameof(MenuDto.Children))]
    public partial MenuDto? ToMenuDto(Menus? source);

    public partial MenuBaseDto? ToMenuBaseDto(Menus? source);
    public partial MenuSearchPickerDto? ToMenuSearchPickerDto(Menus? source);
    public partial List<MenuDto> ToMenuDtos(IEnumerable<Menus>? source);
    public partial List<MenuBaseDto> ToMenuBaseDtos(IEnumerable<Menus>? source);
    public partial List<MenuSearchPickerDto> ToMenuSearchPickerDtos(IEnumerable<Menus>? source);

    [MapperIgnoreTarget(nameof(MenuSearchPickerDto.DisplayNames))]
    public partial MenuSearchPickerDto? ToMenuSearchPickerFromBase(MenuBaseDto? source);
    public partial List<MenuSearchPickerDto> ToMenuSearchPickersFromBase(IEnumerable<MenuBaseDto>? source);

    // Preserve Newtonsoft serialization, including the literal "null" for null values.
    public Operation? ToOperation(MetricsInfo? source)
    {
        if (source is null)
            return null;
        var result = MapOperationCore(source);
        result.Inputs = JsonConvert.SerializeObject(source.Inputs);
        result.ReturnValue = JsonConvert.SerializeObject(source.ReturnValue);
        return result;
    }

    [MapperIgnoreTarget(nameof(Operation.Inputs))]
    [MapperIgnoreTarget(nameof(Operation.ReturnValue))]
    private partial Operation MapOperationCore(MetricsInfo source);
}
