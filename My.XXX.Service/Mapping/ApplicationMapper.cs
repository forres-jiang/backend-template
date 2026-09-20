#nullable enable
using My.XXX.Service.DTOs;
using Riok.Mapperly.Abstractions;
using System.Collections.Generic;
namespace My.XXX.Service.Mapping;
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class ApplicationMapper
{
    [MapperIgnoreTarget(nameof(MenuDto.Checked))]
    [MapperIgnoreTarget(nameof(MenuDto.Actions))]
    [MapperIgnoreTarget(nameof(MenuDto.Children))]
    public partial MenuDto ToMenuDto(MenuBase source);
    public partial List<MenuDto> ToMenuDtos(IEnumerable<MenuBase> source);
    public partial MenuBaseDto ToMenuBaseDto(MenuBase source);
    public partial List<MenuBaseDto> ToMenuBaseDtos(IEnumerable<MenuBase> source);
    public partial List<MenuBase> ToMenuBases(IEnumerable<MenuBase> source);
    [MapperIgnoreTarget(nameof(MenuSearchPickerDto.DisplayNames))]
    public partial MenuSearchPickerDto ToMenuSearchPickerFromBase(MenuBaseDto source);
    public partial List<MenuSearchPickerDto> ToMenuSearchPickersFromBase(IEnumerable<MenuBaseDto> source);
}
